document.querySelectorAll('form[data-confirm]').forEach((form) => {
    form.addEventListener('submit', (event) => {
        if (!window.confirm(form.dataset.confirm)) {
            event.preventDefault();
        }
    });
});

const themeButton = document.getElementById('theme-toggle');

function setTheme(dark) {
    document.documentElement.dataset.theme = dark ? 'dark' : 'light';
    if (themeButton) {
        themeButton.textContent = dark ? 'Light theme' : 'Dark theme';
        themeButton.setAttribute('aria-pressed', String(dark));
    }
}

let savedTheme = 'light';
try {
    savedTheme = localStorage.getItem('dayflow-theme') || 'light';
} catch (e) { }

setTheme(savedTheme === 'dark');

themeButton?.addEventListener('click', () => {
    const dark = document.documentElement.dataset.theme !== 'dark';
    setTheme(dark);
    try {
        localStorage.setItem('dayflow-theme', dark ? 'dark' : 'light');
    } catch (e) { }
});
