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

    // Start Visualizer
    startVisualizer();

    document.getElementById('recordBtn').style.background = 'var(--accent-pink)';
    document.getElementById('recordBtn').textContent = '⏹️';
    document.getElementById('recordStatus').textContent = 'Đang nghe...';

    // Hide previous results
    document.getElementById('spokenArea').classList.add('hidden');
    document.getElementById('scoreArea').classList.add('hidden');
}

function stopRecording() {
    isRecording = false;
    stopVisualizer(); // Stop Visualizer

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

    // Detailed Words Result
    const detailedDiv = document.getElementById('detailedResult');
    if (data.words && data.words.length > 0) {
        detailedDiv.innerHTML = data.words.map(w =>
            `<span style="color: ${w.correct ? 'var(--accent-green)' : 'var(--accent-pink)'}; margin: 0 4px; display: inline-block;">
                ${escapeHtml(w.word)}
                ${!w.correct ? `<br><small style="font-size: 0.8rem; color: var(--text-muted);">${w.error || 'Check this'}</small>` : ''}
            </span>`
        ).join('');
    } else {
        detailedDiv.textContent = '';
    }

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

// --- Visualizer Logic ---
let audioContext, analyser, microphone, dataArray, canvasCtx, visualizerId;
async function startVisualizer() {
    try {
        const stream = await navigator.mediaDevices.getUserMedia({
            audio: { echoCancellation: true, noiseSuppression: true }
        });

        audioContext = new (window.AudioContext || window.webkitAudioContext)();
        analyser = audioContext.createAnalyser();
        microphone = audioContext.createMediaStreamSource(stream);
        microphone.connect(analyser);
        analyser.fftSize = 64; // Low FFT size for simple bars

        const bufferLength = analyser.frequencyBinCount;
        dataArray = new Uint8Array(bufferLength);

        const canvas = document.getElementById('audioVisualizer');
        canvasCtx = canvas.getContext('2d');

        drawVisualizer();
    } catch (err) {
        console.error("Visualizer init error", err);
    }
}

function stopVisualizer() {
    if (microphone) {
        microphone.mediaStream.getTracks().forEach(track => track.stop());
        microphone.disconnect();
    }
    if (audioContext && audioContext.state !== 'closed') audioContext.close();
    if (visualizerId) cancelAnimationFrame(visualizerId);

    // Clear canvas
    const canvas = document.getElementById('audioVisualizer');
    if (canvas) {
        const ctx = canvas.getContext('2d');
        ctx.clearRect(0, 0, canvas.width, canvas.height);
    }
}

function drawVisualizer() {
    visualizerId = requestAnimationFrame(drawVisualizer);
    if (!analyser) return;

    analyser.getByteFrequencyData(dataArray);

    const canvas = document.getElementById('audioVisualizer');
    if (!canvas) return;

    const width = canvas.width;
    const height = canvas.height;

    canvasCtx.clearRect(0, 0, width, height);

    // Filter Noise Gate
    let sum = 0;
    for (let i = 0; i < dataArray.length; i++) sum += dataArray[i];
    const average = sum / dataArray.length;

    if (average < 10) return; // Noise threshold to ignore silence/background noise

    const barWidth = (width / dataArray.length) * 2.5;
    let barHeight;
    let x = 0;

    for (let i = 0; i < dataArray.length; i++) {
        barHeight = dataArray[i] / 2; // Scale down

        // Gradient color
        const gradient = canvasCtx.createLinearGradient(0, 0, 0, height);
        gradient.addColorStop(0, '#4ade80');
        gradient.addColorStop(1, '#3b82f6');

        canvasCtx.fillStyle = gradient;
        canvasCtx.fillRect(x, height - barHeight, barWidth, barHeight);

        x += barWidth + 1;
    }
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}
