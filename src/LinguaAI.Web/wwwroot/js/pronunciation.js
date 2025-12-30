// Pronunciation Mode JavaScript
let isRecording = false;
let recognition = null;
let currentPhrases = [];
let currentPhraseIndex = 0;

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    const savedLang = localStorage.getItem('selectedLanguage') || 'ko';
    document.getElementById('languageSelect').value = savedLang;
    initSpeechRecognition();
    loadNewPhrase();
});

function initSpeechRecognition() {
    if ('webkitSpeechRecognition' in window || 'SpeechRecognition' in window) {
        const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
        recognition = new SpeechRecognition();
        recognition.continuous = false;
        recognition.interimResults = false;

        recognition.onresult = (event) => {
            const spokenText = event.results[0][0].transcript;
            document.getElementById('spokenText').textContent = spokenText;
            document.getElementById('spokenArea').classList.remove('hidden');
            evaluatePronunciation(spokenText);
        };

        recognition.onerror = (event) => {
            console.error('Speech recognition error:', event.error);
            stopRecording();
            document.getElementById('recordStatus').textContent = 'Lỗi nhận dạng. Vui lòng thử lại.';
        };

        recognition.onend = () => {
            stopRecording();
        };
    } else {
        document.getElementById('recordBtn').disabled = true;
        document.getElementById('recordStatus').textContent = 'Trình duyệt không hỗ trợ nhận dạng giọng nói';
    }
}

async function loadNewPhrase() {
    const language = document.getElementById('languageSelect').value;

    try {
        const response = await fetch(`/Practice/GetPhrases?language=${language}`);
        currentPhrases = await response.json();
        currentPhraseIndex = Math.floor(Math.random() * currentPhrases.length);
        displayCurrentPhrase();
    } catch (error) {
        console.error('Error loading phrases:', error);
    }

    // Reset state
    document.getElementById('spokenArea').classList.add('hidden');
    document.getElementById('scoreArea').classList.add('hidden');
}

function displayCurrentPhrase() {
    const phrase = currentPhrases[currentPhraseIndex];
    if (phrase) {
        document.getElementById('phraseText').textContent = phrase.text;
        document.getElementById('phraseRomanization').textContent = phrase.romanization || '';
        document.getElementById('phraseMeaning').textContent = phrase.meaning;
    }
}

function toggleRecording() {
    if (isRecording) {
        recognition.stop();
        stopRecording();
    } else {
        startRecording();
    }
}

function startRecording() {
    isRecording = true;
    const language = document.getElementById('languageSelect').value;

    // Set recognition language
    const langMap = { 'ko': 'ko-KR', 'zh': 'zh-CN', 'en': 'en-US' };
    recognition.lang = langMap[language] || 'en-US';

    recognition.start();

    document.getElementById('recordBtn').style.background = 'var(--accent-pink)';
    document.getElementById('recordBtn').textContent = '⏹️';
    document.getElementById('recordStatus').textContent = 'Đang nghe...';

    // Hide previous results
    document.getElementById('spokenArea').classList.add('hidden');
    document.getElementById('scoreArea').classList.add('hidden');
}

function stopRecording() {
    isRecording = false;
    document.getElementById('recordBtn').style.background = '';
    document.getElementById('recordBtn').textContent = '🎤';
    document.getElementById('recordStatus').textContent = 'Nhấn để ghi âm';
}

async function evaluatePronunciation(spokenText) {
    const language = document.getElementById('languageSelect').value;
    const targetText = document.getElementById('phraseText').textContent;

    document.getElementById('loadingArea').classList.remove('hidden');
    document.getElementById('scoreArea').classList.add('hidden');

    try {
        const response = await fetch('/Practice/EvaluatePronunciation', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ language, targetText, spokenText })
        });

        const data = await response.json();
        displayScore(data);
    } catch (error) {
        console.error('Evaluation error:', error);
        document.getElementById('feedbackText').textContent = 'Lỗi đánh giá. Vui lòng thử lại.';
        document.getElementById('scoreArea').classList.remove('hidden');
    }

    document.getElementById('loadingArea').classList.add('hidden');
}

function displayScore(data) {
    const scoreCircle = document.getElementById('scoreCircle');
    scoreCircle.style.setProperty('--score', data.score);
    document.getElementById('scoreValue').textContent = data.score;
    document.getElementById('feedbackText').textContent = data.feedback;

    const correctionsDiv = document.getElementById('corrections');
    if (data.corrections && data.corrections.length > 0) {
        correctionsDiv.innerHTML = data.corrections.map(c =>
            `<p style="color: var(--accent-gold); font-size: 0.9rem;">💡 ${c}</p>`
        ).join('');
    } else {
        correctionsDiv.innerHTML = '';
    }

    document.getElementById('scoreArea').classList.remove('hidden');

    // Animate score
    let currentScore = 0;
    const targetScore = data.score;
    const interval = setInterval(() => {
        currentScore += 2;
        if (currentScore >= targetScore) {
            currentScore = targetScore;
            clearInterval(interval);
        }
        scoreCircle.style.setProperty('--score', currentScore);
        document.getElementById('scoreValue').textContent = currentScore;
    }, 20);
}
