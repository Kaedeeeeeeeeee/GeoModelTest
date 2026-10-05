/* 保護者同意, then 生徒の同意: the guardian answers on this page, then the student confirms,
 * before the game starts. Loaded in <head> so the form shows before first paint while Unity
 * downloads behind it. Unity reads the answers through GuardianConsent.jslib; the server
 * stores them at New Game. */
(function () {
  'use strict';

  // Bump when the approved wording changes so an older answer is asked again.
  var VERSION = 'guardian-ja-2026-09-29b';
  // Session scope: survives reloads and crash recovery in this tab, not a new visit.
  var STORAGE_KEY = 'geomodel-guardian-consent';
  var MESSAGES = {
    choice: '「同意します」または「同意しません」を選んでください。',
    guardianConfirmed: '「はい」にチェックしてください。',
    guardianNameMissing: '保護者氏名を入力してください。',
    guardianNameInvalid: '保護者氏名を確認してください。',
    respondentIdMissing: 'お子様の回答者IDを入力してください。',
    respondentIdInvalid: '回答者IDは数字（1〜10桁）で入力してください。'
  };
  var root = document.documentElement;
  var memory = null;
  // The guardian's answer is kept for the tab; the student confirms again on every page load,
  // as on the title screen before (nothing about the student is stored).
  var guardianPending = !load();
  var studentAssentedAt = '';
  var pending = true;
  root.classList.add('guardian-consent-pending');

  function load() {
    try {
      var saved = JSON.parse(window.sessionStorage.getItem(STORAGE_KEY) || 'null');
      if (saved && saved.consentVersion === VERSION && saved.guardianAgreed === true) {
        memory = saved;
        return saved;
      }
    } catch (_) { /* Private or partitioned storage: keep the answer in memory only. */ }
    return null;
  }

  function save(payload) {
    memory = payload;
    try { window.sessionStorage.setItem(STORAGE_KEY, JSON.stringify(payload)); } catch (_) {}
  }

  // Testee IDs are digits only, 1 to 10 of them; the length differs per person (Testee,
  // 2026-10-02). Japanese keyboards may give full-width digits; spaces and dashes are typing
  // aids. Leading zeros are kept: the ID is text, not a number.
  function normalizeRespondentId(value) {
    return String(value == null ? '' : value).normalize('NFKC').replace(/[\s‐-―−ー-]/g, '');
  }

  function respondentIdError(value) {
    var id = normalizeRespondentId(value);
    if (!id) return MESSAGES.respondentIdMissing;
    return /^[0-9]{1,10}$/.test(id) ? '' : MESSAGES.respondentIdInvalid;
  }

  function evaluate(values, now) {
    var errors = {};
    var payload = null;
    if (values.choice !== 'yes' && values.choice !== 'no') errors.choice = MESSAGES.choice;
    if (values.choice === 'yes') {
      // trim() also removes the full-width spaces a Japanese IME inserts.
      var name = String(values.guardianName == null ? '' : values.guardianName).trim();
      var respondentId = normalizeRespondentId(values.respondentId);
      if (!values.guardianConfirmed) errors.guardianConfirmed = MESSAGES.guardianConfirmed;
      if (!name) errors.guardianName = MESSAGES.guardianNameMissing;
      else if (name.length > 100 || /[\u0000-\u001f\u007f-\u009f]/.test(name)) errors.guardianName = MESSAGES.guardianNameInvalid;
      var idError = respondentIdError(values.respondentId);
      if (idError) errors.respondentId = idError;
      if (Object.keys(errors).length === 0) {
        payload = {
          consentVersion: VERSION,
          guardianAgreed: true,
          guardianConfirmed: true,
          guardianName: name,
          respondentId: respondentId,
          guardianConsentedAt: now.toISOString()
        };
      }
    }
    return { errors: errors, declined: values.choice === 'no' && Object.keys(errors).length === 0, payload: payload };
  }

  // The student confirms all three statements; nothing else is asked.
  function studentReady(checks) {
    return checks.length === 3 && checks.every(function (checked) { return checked === true; });
  }

  function formatDate(date) {
    return date.getFullYear() + '年' + (date.getMonth() + 1) + '月' + date.getDate() + '日';
  }

  function respondentIdFromUrl() {
    try {
      var params = new URLSearchParams(window.location.search);
      return normalizeRespondentId(params.get('rid') || params.get('respondent_id') || '');
    } catch (_) {
      return '';
    }
  }

  // Unity listens for keys on the whole window and would swallow the guardian's typing.
  // Capture listeners registered here run before Unity's, which are added later.
  // Set by wire(): the form's own Enter handling, which must run inside this shield.
  var enterKey = null;
  ['keydown', 'keypress', 'keyup'].forEach(function (type) {
    window.addEventListener(type, function (event) {
      if (!pending) return;
      event.stopImmediatePropagation();
      if (type === 'keydown' && enterKey) enterKey(event);
    }, true);
  });

  function wire() {
    var overlay = document.getElementById('guardian-consent');
    var form = document.getElementById('guardian-consent-form');
    if (!overlay || !form) return;
    var formView = document.getElementById('guardian-consent-form-view');
    var declinedView = document.getElementById('guardian-consent-declined');
    var studentView = document.getElementById('student-consent-view');
    var studentDeclinedView = document.getElementById('student-consent-declined');
    var studentForm = document.getElementById('student-consent-form');
    var studentChecks = studentForm ? Array.prototype.slice.call(studentForm.querySelectorAll('input[name="studentAgreement"]')) : [];
    var studentSubmit = studentForm ? studentForm.querySelector('button[type="submit"]') : null;
    var studentHint = document.getElementById('student-consent-hint');
    var views = [formView, declinedView, studentView, studentDeclinedView].filter(Boolean);
    var fields = {
      choice: form.querySelector('[data-error-for="choice"]'),
      guardianConfirmed: form.querySelector('[data-error-for="guardianConfirmed"]'),
      guardianName: form.querySelector('[data-error-for="guardianName"]'),
      respondentId: form.querySelector('[data-error-for="respondentId"]')
    };
    var inputs = {
      choice: form.querySelector('input[name="guardianAgreed"]'),
      guardianConfirmed: form.elements.guardianConfirmed,
      guardianName: form.elements.guardianName,
      respondentId: form.elements.respondentId
    };
    var submit = form.querySelector('button[type="submit"]');
    var hint = document.getElementById('guardian-consent-hint');
    var textFields = [inputs.guardianName, inputs.respondentId];
    var lastFocus = null;

    document.getElementById('guardian-consent-date').textContent = formatDate(new Date());
    var prefilled = respondentIdFromUrl();
    if (prefilled) inputs.respondentId.value = prefilled;

    function values() {
      var choice = form.querySelector('input[name="guardianAgreed"]:checked');
      return {
        choice: choice ? choice.value : '',
        guardianConfirmed: inputs.guardianConfirmed.checked,
        guardianName: inputs.guardianName.value,
        respondentId: inputs.respondentId.value
      };
    }

    function showErrors(errors) {
      Object.keys(fields).forEach(function (key) {
        var message = fields[key];
        message.textContent = errors[key] || '';
        message.hidden = !errors[key];
        var input = inputs[key];
        if (input) input.setAttribute('aria-invalid', errors[key] ? 'true' : 'false');
      });
    }

    function show(view) {
      views.forEach(function (candidate) { candidate.hidden = candidate !== view; });
      overlay.scrollTop = 0;
      var heading = view.querySelector('h1');
      if (heading) {
        if (heading.id) overlay.setAttribute('aria-labelledby', heading.id);
        heading.focus({ preventScroll: true });
      }
    }

    function guardianAnswered(payload) {
      save(payload);
      guardianPending = false;
      show(studentView);
    }

    function complete() {
      pending = false;
      root.classList.remove('guardian-consent-pending');
      document.dispatchEvent(new CustomEvent('geomodel:guardian-consent-complete'));
      var canvas = document.getElementById('unity-canvas');
      try { if (canvas) canvas.focus({ preventScroll: true }); } catch (_) {}
    }

    form.addEventListener('submit', function (event) {
      event.preventDefault();
      var result = evaluate(values(), new Date());
      showErrors(result.errors);
      var firstError = Object.keys(fields).filter(function (key) { return result.errors[key]; })[0];
      if (firstError) {
        inputs[firstError].focus();
        return;
      }
      if (result.declined) show(declinedView);
      else guardianAnswered(result.payload);
    });

    function refreshStudent() {
      var ready = studentReady(studentChecks.map(function (input) { return input.checked; }));
      studentSubmit.disabled = !ready;
      studentHint.hidden = ready;
    }
    studentForm.addEventListener('change', refreshStudent);
    studentForm.addEventListener('submit', function (event) {
      event.preventDefault();
      // Guard the action too: a direct submit must never bypass the checkboxes.
      if (!studentReady(studentChecks.map(function (input) { return input.checked; }))) return;
      studentAssentedAt = new Date().toISOString();
      complete();
    });
    document.getElementById('student-consent-decline').addEventListener('click', function () {
      show(studentDeclinedView);
    });
    document.getElementById('student-consent-back').addEventListener('click', function () {
      show(studentView);
    });
    refreshStudent();
    if (!guardianPending) show(studentView);

    // The answer button stays disabled until everything the chosen answer needs is filled in,
    // so neither the button nor the keyboard's Enter can send an incomplete form.
    function refresh() {
      var errors = evaluate(values(), new Date()).errors;
      // Clear a field's message as soon as it is corrected.
      Object.keys(fields).forEach(function (key) {
        if (!fields[key].hidden && !errors[key]) {
          fields[key].hidden = true;
          if (inputs[key]) inputs[key].setAttribute('aria-invalid', 'false');
        }
      });
      var ready = Object.keys(errors).length === 0;
      submit.disabled = !ready;
      hint.hidden = ready;
    }
    form.addEventListener('input', refresh);
    form.addEventListener('change', refresh);
    refresh();

    inputs.respondentId.addEventListener('blur', function () {
      inputs.respondentId.value = normalizeRespondentId(inputs.respondentId.value);
      // With the button disabled there is no submit-time message, so explain a mistyped ID here.
      var error = inputs.respondentId.value ? respondentIdError(inputs.respondentId.value) : '';
      fields.respondentId.textContent = error;
      fields.respondentId.hidden = !error;
      inputs.respondentId.setAttribute('aria-invalid', error ? 'true' : 'false');
    });

    // Enter moves to the next text field and closes the keyboard after the last one; only the
    // answer button submits. An Enter that confirms an IME conversion is left to the IME.
    enterKey = function (event) {
      if (event.key !== 'Enter' || event.isComposing || event.keyCode === 229) return;
      var index = textFields.indexOf(event.target);
      if (index < 0) return;
      event.preventDefault();
      if (index + 1 < textFields.length) textFields[index + 1].focus();
      else event.target.blur();
    };

    document.getElementById('guardian-consent-back').addEventListener('click', function () {
      show(formView);
    });

    // Unity can focus its canvas once it starts; keep the guardian's cursor in the form.
    document.addEventListener('focusin', function (event) {
      if (!pending) return;
      if (overlay.contains(event.target)) {
        lastFocus = event.target;
        return;
      }
      if (lastFocus && document.contains(lastFocus)) lastFocus.focus({ preventScroll: true });
    }, true);
  }

  window.GeoModelGuardianConsent = {
    version: VERSION,
    isPending: function () { return pending; },
    isGuardianPending: function () { return guardianPending; },
    // Consumed by GuardianConsent.jslib. Empty until the guardian has agreed and the student confirmed.
    payloadJson: function () {
      var payload = memory || load();
      if (!payload || !studentAssentedAt) return '';
      var answer = {};
      Object.keys(payload).forEach(function (key) { answer[key] = payload[key]; });
      answer.studentAssented = true;
      answer.studentAssentedAt = studentAssentedAt;
      return JSON.stringify(answer);
    },
    // Called when the server rejects the stored answer: the next page load asks again.
    reset: function () {
      memory = null;
      try { window.sessionStorage.removeItem(STORAGE_KEY); } catch (_) {}
    },
    normalizeRespondentId: normalizeRespondentId,
    evaluate: evaluate,
    studentReady: studentReady
  };

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', wire);
  else wire();
}());
