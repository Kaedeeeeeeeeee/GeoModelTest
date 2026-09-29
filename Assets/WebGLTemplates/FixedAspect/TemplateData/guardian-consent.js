/* 保護者同意: the guardian answers on this page before the child starts the game.
 * Loaded in <head> so the form shows before first paint while Unity downloads behind it.
 * Unity reads the answer through GuardianConsent.jslib; the server stores it at New Game. */
(function () {
  'use strict';

  // Bump when the approved wording changes so an older answer is asked again.
  var VERSION = 'guardian-ja-2026-09-29';
  // Session scope: survives reloads and crash recovery in this tab, not a new visit.
  var STORAGE_KEY = 'geomodel-guardian-consent';
  var MESSAGES = {
    choice: '「同意します」または「同意しません」を選んでください。',
    guardianConfirmed: '「はい」にチェックしてください。',
    guardianNameMissing: '保護者氏名を入力してください。',
    guardianNameInvalid: '保護者氏名を確認してください。',
    respondentIdMissing: 'お子様の回答者IDを入力してください。',
    respondentIdInvalid: '回答者IDは半角の英数字で入力してください。'
  };
  var root = document.documentElement;
  var memory = null;
  var pending = !load();
  if (pending) root.classList.add('guardian-consent-pending');

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

  // Japanese keyboards often produce full-width characters and long-vowel marks for "-".
  function normalizeRespondentId(value) {
    return String(value == null ? '' : value).normalize('NFKC')
      .replace(/[‐-―−ー]/g, '-')
      .replace(/\s+/g, '');
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
      if (!respondentId) errors.respondentId = MESSAGES.respondentIdMissing;
      else if (!/^[A-Za-z0-9._-]{1,64}$/.test(respondentId)) errors.respondentId = MESSAGES.respondentIdInvalid;
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
  ['keydown', 'keypress', 'keyup'].forEach(function (type) {
    window.addEventListener(type, function (event) {
      if (pending) event.stopImmediatePropagation();
    }, true);
  });

  function wire() {
    var overlay = document.getElementById('guardian-consent');
    var form = document.getElementById('guardian-consent-form');
    if (!overlay || !form) return;
    var formView = document.getElementById('guardian-consent-form-view');
    var declinedView = document.getElementById('guardian-consent-declined');
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
      formView.hidden = view !== formView;
      declinedView.hidden = view !== declinedView;
      overlay.scrollTop = 0;
      var heading = view.querySelector('h1');
      if (heading) heading.focus({ preventScroll: true });
    }

    function complete(payload) {
      save(payload);
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
      else complete(result.payload);
    });

    // Clear a field's message as soon as it is corrected.
    form.addEventListener('change', function () {
      var result = evaluate(values(), new Date());
      Object.keys(fields).forEach(function (key) {
        if (!fields[key].hidden && !result.errors[key]) {
          fields[key].hidden = true;
          if (inputs[key]) inputs[key].setAttribute('aria-invalid', 'false');
        }
      });
    });
    inputs.respondentId.addEventListener('blur', function () {
      inputs.respondentId.value = normalizeRespondentId(inputs.respondentId.value);
    });

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
    // Consumed by GuardianConsent.jslib. Empty until the guardian has agreed.
    payloadJson: function () {
      var payload = memory || load();
      return payload ? JSON.stringify(payload) : '';
    },
    // Called when the server rejects the stored answer: the next page load asks again.
    reset: function () {
      memory = null;
      try { window.sessionStorage.removeItem(STORAGE_KEY); } catch (_) {}
    },
    normalizeRespondentId: normalizeRespondentId,
    evaluate: evaluate
  };

  if (!pending) return;
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', wire);
  else wire();
}());
