/* Teacher preview: no ticket, storage, API client, or submission endpoint. */
(() => {
  'use strict';
  const $ = id => document.getElementById(id);
  const definition = window.GEOMODEL_QUESTIONS;
  const answers = {};
  let step = 0;

  $('definition-version').textContent = '設問バージョン: ' + definition.version;
  $('intro').textContent = definition.intro;
  $('privacy').textContent = definition.privacy;
  $('progress').max = definition.sections.length;

  function updateCount() {
    const count = definition.questions.filter(q => answers[q.id]?.trim()).length;
    $('answered-label').textContent = `${count} / ${definition.questions.length} 問`;
  }

  function show(focus = false) {
    $('preview-complete').hidden = true;
    $('preview-form').hidden = false;
    $('step-label').textContent = `${step + 1} / ${definition.sections.length} ページ`;
    $('progress').value = step + 1;
    definition.sections.forEach((_, index) => {
      $('section-' + index).hidden = index !== step;
    });
    $('form-error').hidden = true;
    $('previous').hidden = step === 0;
    $('next').textContent = step === definition.sections.length - 1 ? '回答を送信する' : '次へ →';
    if (focus) {
      $('heading-' + step).focus();
      $('progress').scrollIntoView({block: 'start'});
    }
    updateCount();
  }

  definition.sections.forEach((section, index) => {
    const panel = document.createElement('section');
    panel.id = 'section-' + index;
    panel.className = 'preview-section';
    const heading = document.createElement('div');
    heading.className = 'section-heading';
    const number = document.createElement('span');
    number.textContent = String(index + 1).padStart(2, '0');
    const title = document.createElement('h2');
    title.id = 'heading-' + index;
    title.tabIndex = -1;
    title.textContent = section.title;
    panel.setAttribute('aria-labelledby', title.id);
    const description = document.createElement('p');
    description.textContent = section.description;
    heading.append(number, title, description);
    panel.append(heading);

    for (const id of section.questions) {
      const question = definition.questions.find(q => q.id === id);
      const field = document.createElement('fieldset');
      field.id = 'field-' + id;
      const legend = document.createElement('legend');
      const number = document.createElement('span');
      number.className = 'number';
      number.textContent = id.slice(1) + '.';
      legend.append(number, document.createTextNode(question.text));
      if (question.optional) {
        const optional = document.createElement('span');
        optional.className = 'optional';
        optional.textContent = '自由回答';
        legend.append(optional);
      }
      field.append(legend);
      if (question.scale) {
        const options = document.createElement('div');
        options.className = 'options';
        for (const [value, text] of definition.scales[question.scale]) {
          const label = document.createElement('label');
          label.className = 'option';
          const input = document.createElement('input');
          input.type = 'radio';
          input.name = id;
          input.value = value;
          input.onchange = () => {
            answers[id] = value;
            field.classList.remove('invalid');
            field.removeAttribute('aria-invalid');
            if (section.questions.every(key => definition.questions.find(q => q.id === key).optional || answers[key])) $('form-error').hidden = true;
            updateCount();
          };
          label.append(input, document.createTextNode(text));
          options.append(label);
        }
        field.append(options);
      } else {
        const input = document.createElement('textarea');
        input.name = id;
        input.maxLength = question.maxLength;
        input.placeholder = '書かなくてもかまいません';
        input.setAttribute('aria-label', question.text);
        const count = document.createElement('p');
        count.className = 'counter';
        count.textContent = `0 / ${question.maxLength} 文字`;
        input.oninput = () => {
          answers[id] = input.value;
          count.textContent = `${input.value.length} / ${question.maxLength} 文字`;
          updateCount();
        };
        field.append(input, count);
      }
      panel.append(field);
    }
    $('sections').append(panel);
  });

  $('previous').onclick = () => { if (step > 0) step--; show(true); };
  $('survey').onsubmit = event => {
    event.preventDefault();
    const missing = definition.sections[step].questions.find(id => !definition.questions.find(q => q.id === id).optional && !answers[id]);
    if (missing) {
      $('field-' + missing).classList.add('invalid');
      $('field-' + missing).setAttribute('aria-invalid', 'true');
      $('form-error').textContent = 'まだ選んでいない質問があります。「答えたくない」も選べます。';
      $('form-error').hidden = false;
      $('form-error').focus();
      return;
    }
    if (step < definition.sections.length - 1) {
      step++;
      show(true);
    } else {
      $('preview-form').hidden = true;
      $('preview-complete').hidden = false;
      $('preview-complete').focus();
      window.scrollTo(0, 0);
    }
  };
  show();
})();
