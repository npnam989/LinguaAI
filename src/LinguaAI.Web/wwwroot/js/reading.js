// Reading Mode JavaScript
let quizData = [];
let userAnswers = {};

document.addEventListener('DOMContentLoaded', () => {
    const savedLang = localStorage.getItem('selectedLanguage') || 'ko';
    document.getElementById('languageSelect').value = savedLang;
});

async function generateReading() {
    const language = document.getElementById('languageSelect').value;
    const level = document.getElementById('levelSelect').value;
    const topic = document.getElementById('topicSelect').value;

    document.getElementById('emptyState').classList.add('hidden');
    document.getElementById('contentArea').classList.add('hidden');
    document.getElementById('loadingArea').classList.remove('hidden');

    try {
        const response = await fetch(`${window.API_BASE_URL}/api/reading/generate`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ language, level, topic: topic || null })
        });

        const data = await response.json();
        displayReading(data);
    } catch (error) {
        console.error('Reading generation error:', error);
        alert('Lỗi tạo bài đọc. Vui lòng thử lại.');
        document.getElementById('emptyState').classList.remove('hidden');
    }

    document.getElementById('loadingArea').classList.add('hidden');
}

function displayReading(data) {
    // Title and content
    document.getElementById('readingTitle').textContent = data.title;
    document.getElementById('readingContent').innerHTML = data.content.replace(/\n/g, '<br><br>');

    // Vocabulary
    const vocabList = document.getElementById('vocabularyList');
    if (data.vocabulary && data.vocabulary.length > 0) {
        vocabList.innerHTML = data.vocabulary.map(v => `
            <div class="glass-panel" style="padding: var(--space-sm) var(--space-md); margin-bottom: var(--space-sm);">
                <p style="font-weight: 600; color: var(--primary-light);">${escapeHtml(v.word)}</p>
                ${v.pronunciation ? `<p style="font-size: 0.75rem; color: var(--text-muted);">${escapeHtml(v.pronunciation)}</p>` : ''}
                <p style="font-size: 0.875rem; color: var(--text-secondary);">${escapeHtml(v.meaning)}</p>
            </div>
        `).join('');
    } else {
        vocabList.innerHTML = '<p style="color: var(--text-muted);">Không có từ vựng</p>';
    }

    // Quiz
    quizData = data.questions || [];
    userAnswers = {};
    const quizArea = document.getElementById('quizArea');
    if (quizData.length > 0) {
        quizArea.innerHTML = quizData.map((q, idx) => `
            <div class="glass-panel" style="padding: var(--space-md); margin-bottom: var(--space-md);">
                <p style="font-weight: 600; margin-bottom: var(--space-sm);">${idx + 1}. ${escapeHtml(q.question)}</p>
                <div style="display: grid; gap: var(--space-sm);">
                    ${q.options.map((opt, optIdx) => `
                        <label class="quiz-option" style="display: flex; align-items: center; gap: var(--space-sm); padding: var(--space-sm); cursor: pointer; border-radius: var(--radius-sm);" data-question="${idx}" data-option="${optIdx}">
                            <input type="radio" name="q${idx}" value="${optIdx}" onchange="selectAnswer(${idx}, ${optIdx})">
                            <span>${escapeHtml(opt)}</span>
                        </label>
                    `).join('')}
                </div>
            </div>
        `).join('');
        document.getElementById('checkAnswersBtn').classList.remove('hidden');
    } else {
        quizArea.innerHTML = '<p style="color: var(--text-muted);">Không có câu hỏi</p>';
        document.getElementById('checkAnswersBtn').classList.add('hidden');
    }

    document.getElementById('quizResult').classList.add('hidden');
    document.getElementById('contentArea').classList.remove('hidden');
}

function selectAnswer(questionIdx, optionIdx) {
    userAnswers[questionIdx] = optionIdx;
}

function checkAnswers() {
    let correct = 0;
    quizData.forEach((q, idx) => {
        const labels = document.querySelectorAll(`[data-question="${idx}"]`);
        labels.forEach((label, optIdx) => {
            label.style.background = '';
            if (optIdx === q.correctIndex) {
                label.style.background = 'hsla(150, 70%, 50%, 0.2)';
                label.style.border = '1px solid var(--accent-green)';
            } else if (userAnswers[idx] === optIdx && optIdx !== q.correctIndex) {
                label.style.background = 'hsla(350, 70%, 50%, 0.2)';
                label.style.border = '1px solid var(--accent-pink)';
            }
        });
        if (userAnswers[idx] === q.correctIndex) {
            correct++;
        }
    });

    const result = document.getElementById('quizResult');
    const percentage = Math.round((correct / quizData.length) * 100);
    result.innerHTML = `
        <div class="glass-panel" style="padding: var(--space-lg); text-align: center;">
            <p style="font-size: 2rem; font-weight: 700; color: ${percentage >= 70 ? 'var(--accent-green)' : 'var(--accent-gold)'};">
                ${correct}/${quizData.length} (${percentage}%)
            </p>
            <p style="color: var(--text-secondary);">
                ${percentage >= 70 ? '🎉 Tuyệt vời!' : percentage >= 50 ? '👍 Khá tốt!' : '💪 Cố gắng thêm nhé!'}
            </p>
        </div>
    `;
    result.classList.remove('hidden');
    document.getElementById('checkAnswersBtn').classList.add('hidden');
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
