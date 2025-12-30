// Writing Mode JavaScript

document.addEventListener('DOMContentLoaded', () => {
    const savedLang = localStorage.getItem('selectedLanguage') || 'ko';
    document.getElementById('languageSelect').value = savedLang;
});

async function checkWriting() {
    const text = document.getElementById('writingInput').value.trim();
    if (!text) {
        alert('Vui lòng nhập bài viết');
        return;
    }

    const language = document.getElementById('languageSelect').value;
    const level = document.getElementById('levelSelect').value;

    document.getElementById('emptyState').classList.add('hidden');
    document.getElementById('resultArea').classList.add('hidden');
    document.getElementById('loadingArea').classList.remove('hidden');

    try {
        const response = await fetch('/Practice/CheckWriting', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ language, text, level })
        });

        const data = await response.json();
        displayResult(data);
    } catch (error) {
        console.error('Writing check error:', error);
        alert('Lỗi kết nối. Vui lòng thử lại.');
    }

    document.getElementById('loadingArea').classList.add('hidden');
}

function displayResult(data) {
    // Corrected text
    document.getElementById('correctedText').innerHTML = escapeHtml(data.correctedText);

    // Corrections list
    const correctionsList = document.getElementById('correctionsList');
    if (data.corrections && data.corrections.length > 0) {
        correctionsList.innerHTML = data.corrections.map(c => `
            <div class="glass-panel" style="padding: var(--space-md); margin-bottom: var(--space-sm);">
                <div style="display: flex; gap: var(--space-md); align-items: center; margin-bottom: var(--space-sm);">
                    <span style="color: var(--accent-pink); text-decoration: line-through;">${escapeHtml(c.original)}</span>
                    <span>→</span>
                    <span style="color: var(--accent-green);">${escapeHtml(c.corrected)}</span>
                </div>
                <p style="font-size: 0.875rem; color: var(--text-muted);">${escapeHtml(c.explanation)}</p>
            </div>
        `).join('');
        document.getElementById('correctionsArea').classList.remove('hidden');
    } else {
        correctionsList.innerHTML = '<p style="color: var(--accent-green);">✅ Không có lỗi nào!</p>';
        document.getElementById('correctionsArea').classList.remove('hidden');
    }

    // Suggestions
    const suggestionsList = document.getElementById('suggestionsList');
    if (data.suggestions && data.suggestions.length > 0) {
        suggestionsList.innerHTML = data.suggestions.map(s =>
            `<li style="margin-bottom: var(--space-sm);">${escapeHtml(s)}</li>`
        ).join('');
        document.getElementById('suggestionsArea').classList.remove('hidden');
    } else {
        document.getElementById('suggestionsArea').classList.add('hidden');
    }

    document.getElementById('resultArea').classList.remove('hidden');
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
