const toggle = document.querySelector('.nav-toggle');
const nav = document.querySelector('.site-nav');
if (toggle && nav) {
  toggle.addEventListener('click', () => {
    const open = nav.classList.toggle('open');
    toggle.setAttribute('aria-expanded', String(open));
  });
  nav.addEventListener('click', () => {
    nav.classList.remove('open');
    toggle.setAttribute('aria-expanded', 'false');
  });
}

const topic = new URLSearchParams(window.location.search).get('topic');
const topicSelect = document.querySelector('#topic');
if (topic && topicSelect) {
  const matchingOption = [...topicSelect.options].find(option => option.value === topic);
  if (matchingOption) topicSelect.value = topic;
}
