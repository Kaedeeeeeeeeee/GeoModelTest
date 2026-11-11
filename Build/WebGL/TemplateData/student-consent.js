/* 生徒の同意: the student confirms three statements on this page before the game starts.
 * The 保護者同意 page that came first was removed on 2026-11-11 (玉川中 checks the game in
 * advance). Loaded in <head> so the sheet shows before first paint while Unity downloads behind
 * it. Unity waits and reads the answer through StudentConsent.jslib; the server stores the
 * assent time at New Game. */
(function () {
  'use strict';

  // Bump when the approved wording changes.
  var VERSION = 'student-ja-2026-11-11';
  var root = document.documentElement;
  // Asked again on every page load, as on the title screen before; kept in memory only.
  var pending = true;
  var assentedAt = '';
  root.classList.add('student-consent-pending');

  // The student confirms all three statements; nothing else is asked.
  function studentReady(checks) {
    return checks.length === 3 && checks.every(function (checked) { return checked === true; });
  }

  // Unity listens for keys on the whole window and would swallow Space on a checkbox.
  // Capture listeners registered here run before Unity's, which are added later.
  ['keydown', 'keypress', 'keyup'].forEach(function (type) {
    window.addEventListener(type, function (event) {
      if (pending) event.stopImmediatePropagation();
    }, true);
  });

  function wire() {
    var overlay = document.getElementById('student-consent');
    var form = document.getElementById('student-consent-form');
    if (!overlay || !form) return;
    var view = document.getElementById('student-consent-view');
    var declinedView = document.getElementById('student-consent-declined');
    var checks = Array.prototype.slice.call(form.querySelectorAll('input[name="studentAgreement"]'));
    var submit = form.querySelector('button[type="submit"]');
    var hint = document.getElementById('student-consent-hint');
    var lastFocus = null;

    function ready() {
      return studentReady(checks.map(function (input) { return input.checked; }));
    }

    function show(target) {
      [view, declinedView].forEach(function (candidate) { candidate.hidden = candidate !== target; });
      overlay.scrollTop = 0;
      var heading = target.querySelector('h1');
      if (heading) {
        if (heading.id) overlay.setAttribute('aria-labelledby', heading.id);
        heading.focus({ preventScroll: true });
      }
    }

    function refresh() {
      var ok = ready();
      submit.disabled = !ok;
      hint.hidden = ok;
    }

    form.addEventListener('change', refresh);
    form.addEventListener('submit', function (event) {
      event.preventDefault();
      // Guard the action too: a direct submit must never bypass the checkboxes.
      if (!ready()) return;
      assentedAt = new Date().toISOString();
      pending = false;
      root.classList.remove('student-consent-pending');
      document.dispatchEvent(new CustomEvent('geomodel:student-consent-complete'));
      var canvas = document.getElementById('unity-canvas');
      try { if (canvas) canvas.focus({ preventScroll: true }); } catch (_) {}
    });
    document.getElementById('student-consent-decline').addEventListener('click', function () {
      show(declinedView);
    });
    document.getElementById('student-consent-back').addEventListener('click', function () {
      show(view);
    });
    refresh();

    // Unity can focus its canvas once it starts; keep the student's focus on the sheet.
    document.addEventListener('focusin', function (event) {
      if (!pending) return;
      if (overlay.contains(event.target)) {
        lastFocus = event.target;
        return;
      }
      if (lastFocus && document.contains(lastFocus)) lastFocus.focus({ preventScroll: true });
    }, true);
  }

  window.GeoModelStudentConsent = {
    isPending: function () { return pending; },
    // Consumed by StudentConsent.jslib. Empty until the student has confirmed.
    payloadJson: function () {
      if (!assentedAt) return '';
      return JSON.stringify({ consentVersion: VERSION, studentAssented: true, studentAssentedAt: assentedAt });
    },
    studentReady: studentReady
  };

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', wire);
  else wire();
}());
