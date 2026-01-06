// Vocabulary Mode JavaScript
let vocabularyList = [];
let currentIndex = 0;
let vocabularySource = 'ai'; // 'ai' or 'file'
let currentMode = 'flashcard'; // 'flashcard', 'quiz', 'practice'

// Practice state
let practiceExercises = [];
let practiceIndex = 0;
let practiceScore = 0;
let currentPracticeType = 'fill_blank';
let originalArrangeSource = []; // For resetting arrange exercise

// Quiz state

let quizIndex = 0;
let quizCorrect = 0;
let quizWrong = 0;
let quizTimer = null;
let quizTimeLeft = 15;
let quizRetriesLeft = 2;
let quizRecognition = null;
const QUIZ_TIME_LIMIT = 15; // seconds

// Translation cache for display meanings
let translationCache = {};
const displayLanguageLabels = {
    'vi': 'Nghĩa tiếng Việt',
    'en': 'English meaning',
    'zh': '中文意思'
};

document.addEventListener('DOMContentLoaded', () => {
    const savedLang = localStorage.getItem('selectedLanguage') || 'ko';
    document.getElementById('languageSelect').value = savedLang;
    initQuizSpeechRecognition();

    // Listen for display language change
    document.getElementById('displayLanguageSelect').addEventListener('change', async () => {
        if (currentMode === 'quiz' && vocabularyList.length > 0) {
            // Clear timer and reload current question with new language
            clearInterval(quizTimer);
            await showQuizQuestion();
        }
    });
});

// Initialize speech recognition for quiz with optimizations
// Audio recording for quiz
let mediaRecorder = null;
let audioChunks = [];
let isRecording = false;
let audioContext;
let analyser;
let visualizerFrame;
let stream;

// Remove initQuickSpeechRecognition as we use MediaRecorder now

function initQuizSpeechRecognition() {
    // Check for MediaRecorder support
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        console.error('MediaRecorder not supported');
        alert('Trình duyệt không hỗ trợ ghi âm.');
        return;
    }
}

// Mode switching
function switchMode(mode) {
    currentMode = mode;
    document.querySelectorAll('.mode-btn').forEach(btn => {
        btn.classList.remove('active');
        btn.classList.remove('btn-primary');
        btn.classList.add('btn-secondary');
    });
    document.querySelector(`[data-mode="${mode}"]`).classList.add('active');
    document.querySelector(`[data-mode="${mode}"]`).classList.remove('btn-secondary');
    document.querySelector(`[data-mode="${mode}"]`).classList.add('btn-primary');

    if (mode === 'flashcard') {
        document.getElementById('flashcardArea').classList.remove('hidden');
        document.getElementById('quizArea').classList.add('hidden');
        document.getElementById('practiceArea').classList.add('hidden');
        document.getElementById('quizDisplayLangSelector').classList.add('hidden');
    } else if (mode === 'practice') {
        document.getElementById('flashcardArea').classList.add('hidden');
        document.getElementById('quizArea').classList.add('hidden');
        document.getElementById('practiceArea').classList.remove('hidden');
        document.getElementById('quizDisplayLangSelector').classList.add('hidden');
    } else {
        document.getElementById('flashcardArea').classList.add('hidden');
        document.getElementById('quizArea').classList.remove('hidden');
        document.getElementById('practiceArea').classList.add('hidden');
        document.getElementById('quizDisplayLangSelector').classList.remove('hidden');
        startQuiz();
    }
}

// Load vocabulary from AI
async function loadVocabulary() {
    const language = document.getElementById('languageSelect').value;
    const theme = document.getElementById('themeSelect').value;

    document.getElementById('emptyState').classList.add('hidden');
    document.getElementById('flashcardArea').classList.add('hidden');
    document.getElementById('quizArea').classList.add('hidden');
    document.getElementById('practiceArea').classList.add('hidden');
    document.getElementById('modeSelection').classList.add('hidden');
    document.getElementById('loadingArea').classList.remove('hidden');

    try {
        const response = await fetch('/Practice/GenerateVocabulary', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ language, theme, count: 10 })
        });

        const data = await response.json();
        vocabularyList = data.words || [];
        currentIndex = 0;
        vocabularySource = 'ai';
        onVocabularyLoaded();
    } catch (error) {
        console.error('Vocabulary loading error:', error);
        alert('Lỗi tải từ vựng. Vui lòng thử lại.');
        document.getElementById('emptyState').classList.remove('hidden');
    }

    document.getElementById('loadingArea').classList.add('hidden');
}

// Handle file upload
async function handleFileUpload(event) {
    const file = event.target.files[0];
    if (!file) return;

    document.getElementById('fileName').textContent = file.name;

    const validTypes = ['.xlsx', '.docx'];
    const ext = '.' + file.name.split('.').pop().toLowerCase();
    if (!validTypes.includes(ext)) {
        alert('Chỉ hỗ trợ file Excel (.xlsx) hoặc Word (.docx)');
        return;
    }

    document.getElementById('emptyState').classList.add('hidden');
    document.getElementById('flashcardArea').classList.add('hidden');
    document.getElementById('quizArea').classList.add('hidden');
    document.getElementById('practiceArea').classList.add('hidden');
    document.getElementById('modeSelection').classList.add('hidden');
    document.getElementById('loadingArea').classList.remove('hidden');

    try {
        const formData = new FormData();
        formData.append('file', file);

        const response = await fetch('/Practice/UploadVocabulary', {
            method: 'POST',
            body: formData
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.error || 'Lỗi upload file');
        }

        if (data.warning) {
            alert(data.warning);
        }

        vocabularyList = data.words || [];
        currentIndex = 0;
        vocabularySource = 'file';
        onVocabularyLoaded();
    } catch (error) {
        console.error('File upload error:', error);
        alert(error.message || 'Lỗi đọc file. Vui lòng kiểm tra định dạng.');
        document.getElementById('emptyState').classList.remove('hidden');
    }

    document.getElementById('loadingArea').classList.add('hidden');
    event.target.value = '';
}

function onVocabularyLoaded() {
    if (vocabularyList.length === 0) {
        document.getElementById('emptyState').classList.remove('hidden');
        return;
    }

    // Show mode selection
    document.getElementById('modeSelection').classList.remove('hidden');

    // Default to flashcard mode
    currentMode = 'flashcard';
    displayVocabulary();
}

function displayVocabulary() {
    if (vocabularyList.length === 0) {
        document.getElementById('emptyState').classList.remove('hidden');
        return;
    }

    document.getElementById('totalCount').textContent = vocabularyList.length;
    document.getElementById('wordCount').textContent = vocabularyList.length;

    const sourceLabel = document.getElementById('sourceLabel');
    if (vocabularySource === 'file') {
        sourceLabel.textContent = '📁 Từ file';
        sourceLabel.className = 'lang-badge chinese';
    } else {
        sourceLabel.textContent = '🤖 Từ AI';
        sourceLabel.className = 'lang-badge korean';
    }

    updateCard();

    const wordListDiv = document.getElementById('wordList');
    wordListDiv.innerHTML = vocabularyList.map((word, idx) => `
        <div class="glass-panel" style="padding: var(--space-sm) var(--space-md); cursor: pointer;" onclick="goToCard(${idx})">
            <p style="font-weight: 600; color: var(--primary-light);">${escapeHtml(word.word)}</p>
            <p style="font-size: 0.875rem; color: var(--text-secondary);">${escapeHtml(word.meaning)}</p>
        </div>
    `).join('');

    document.getElementById('flashcardArea').classList.remove('hidden');
}

function updateCard() {
    const word = vocabularyList[currentIndex];
    if (!word) return;

    document.getElementById('currentIndex').textContent = currentIndex + 1;
    document.getElementById('cardWord').textContent = word.word;
    document.getElementById('cardPronunciation').textContent = word.pronunciation || '';
    document.getElementById('cardMeaning').textContent = word.meaning;
    document.getElementById('cardExample').textContent = word.example || '';

    document.getElementById('flashcard').classList.remove('flipped');
}

function flipCard() {
    document.getElementById('flashcard').classList.toggle('flipped');
}

function nextCard() {
    currentIndex = (currentIndex + 1) % vocabularyList.length;
    updateCard();
}

function prevCard() {
    currentIndex = (currentIndex - 1 + vocabularyList.length) % vocabularyList.length;
    updateCard();
}

function shuffleCards() {
    for (let i = vocabularyList.length - 1; i > 0; i--) {
        const j = Math.floor(Math.random() * (i + 1));
        [vocabularyList[i], vocabularyList[j]] = [vocabularyList[j], vocabularyList[i]];
    }
    currentIndex = 0;
    displayVocabulary();
}

function goToCard(index) {
    currentIndex = index;
    updateCard();
    document.querySelector('.flashcard-container').scrollIntoView({ behavior: 'smooth', block: 'center' });
}

// ==================== SPEAKING QUIZ ====================

function startQuiz() {
    quizIndex = 0;
    quizCorrect = 0;
    quizWrong = 0;

    // Shuffle for quiz
    for (let i = vocabularyList.length - 1; i > 0; i--) {
        const j = Math.floor(Math.random() * (i + 1));
        [vocabularyList[i], vocabularyList[j]] = [vocabularyList[j], vocabularyList[i]];
    }

    document.getElementById('quizScoreSummary').classList.add('hidden');
    document.getElementById('quizTotalCount').textContent = vocabularyList.length;

    showQuizQuestion();
}

async function showQuizQuestion() {
    if (quizIndex >= vocabularyList.length) {
        showQuizSummary();
        return;
    }

    // Clear any existing timer first
    if (quizTimer) {
        clearInterval(quizTimer);
        quizTimer = null;
    }

    const word = vocabularyList[quizIndex];
    const displayLang = document.getElementById('displayLanguageSelect').value;

    document.getElementById('quizCurrentIndex').textContent = quizIndex + 1;

    // Update label
    document.getElementById('quizMeaningLabel').textContent = displayLanguageLabels[displayLang] + ':';

    // Always translate meaning to selected language
    let displayMeaning = word.meaning;

    if (word.meaning) {
        displayMeaning = await getTranslatedMeaning(word.meaning, displayLang);
    }

    document.getElementById('quizMeaning').textContent = displayMeaning;
    document.getElementById('quizHint').textContent = `Gợi ý: ${word.pronunciation || '...'}`;
    document.getElementById('quizHint').classList.add('hidden');

    document.getElementById('quizResultArea').classList.add('hidden');
    document.getElementById('quizRecordBtn').disabled = false;
    document.getElementById('quizRecordStatus').textContent = 'Nhấn để trả lời';

    // Reset retries
    quizRetriesLeft = 2;
    document.getElementById('quizRetryCount').textContent = quizRetriesLeft;
    document.getElementById('quizRetryBtn').classList.add('hidden');

    // Start timer
    startQuizTimer();
}

function retryQuizQuestion() {
    if (quizRetriesLeft <= 0) return;

    quizRetriesLeft--;
    document.getElementById('quizRetryCount').textContent = quizRetriesLeft;

    // Reset UI for retry
    document.getElementById('quizResultWrapper').classList.add('hidden');
    document.getElementById('quizResultArea').classList.add('hidden');
    document.getElementById('quizRecordBtn').disabled = false;
    document.getElementById('quizRecordStatus').textContent = 'Nhấn để trả lời (Thử lại)';

    // Clear visualizer
    const canvas = document.getElementById('quizVisualizer');
    const ctx = canvas.getContext('2d');
    ctx.clearRect(0, 0, canvas.width, canvas.height);

    // Restart timer
    startQuizTimer();
}

// Get translated meaning using API
async function getTranslatedMeaning(meaning, targetLang) {
    const cacheKey = `${meaning}_${targetLang}`;

    if (translationCache[cacheKey]) {
        return translationCache[cacheKey];
    }

    try {
        const response = await fetch('/Practice/Translate', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ text: meaning, targetLanguage: targetLang })
        });

        if (response.ok) {
            const data = await response.json();
            translationCache[cacheKey] = data.translated;
            return data.translated;
        }
    } catch (error) {
        console.error('Translation error:', error);
    }

    // Fallback to original
    translationCache[cacheKey] = meaning;
    return meaning;
}

function startQuizTimer() {
    // Clear any existing timer
    if (quizTimer) {
        clearInterval(quizTimer);
        quizTimer = null;
    }

    quizTimeLeft = QUIZ_TIME_LIMIT;
    updateTimerDisplay();

    quizTimer = setInterval(() => {
        quizTimeLeft--;
        updateTimerDisplay();

        if (quizTimeLeft <= 0) {
            clearInterval(quizTimer);
            quizTimer = null;
            timeUp();
        }
    }, 1000);
}

function updateTimerDisplay() {
    document.getElementById('timerDisplay').textContent = quizTimeLeft;
    const percentage = (quizTimeLeft / QUIZ_TIME_LIMIT) * 100;
    document.getElementById('timerBar').style.width = percentage + '%';

    if (quizTimeLeft <= 3) {
        document.getElementById('timerDisplay').style.color = 'var(--accent-pink)';
    } else {
        document.getElementById('timerDisplay').style.color = 'var(--accent-gold)';
    }
}

function timeUp() {
    stopQuizRecording();
    document.getElementById('quizResultText').textContent = 'Hết giờ!';
    document.getElementById('quizResultText').style.color = 'var(--accent-pink)';
    document.getElementById('quizCorrectAnswer').textContent = `Đáp án: ${word.word} (${word.pronunciation || ''})`;
    document.getElementById('quizResultArea').classList.remove('hidden');
    document.getElementById('quizRecordBtn').disabled = true;
}

// Audio recording & Visualizer


async function toggleQuizRecording() {
    if (isRecording) {
        await stopQuizRecordingAndSubmit();
    } else {
        await startQuizRecording();
    }
}

async function startQuizRecording() {
    if (isRecording) return;

    // Hide previous recording buttons (for re-recording)
    document.getElementById('recordingActions').classList.add('hidden');

    // Reset and restart timer
    clearInterval(quizTimer);
    quizTimeLeft = QUIZ_TIME_LIMIT;
    document.getElementById('timerDisplay').textContent = quizTimeLeft;
    document.getElementById('timerBar').style.width = '100%';
    quizTimer = setInterval(() => {
        quizTimeLeft--;
        document.getElementById('timerDisplay').textContent = quizTimeLeft;
        document.getElementById('timerBar').style.width = `${(quizTimeLeft / QUIZ_TIME_LIMIT) * 100}%`;
        if (quizTimeLeft <= 0) {
            clearInterval(quizTimer);
            if (isRecording) {
                stopQuizRecording();
            }
        }
    }, 1000);

    // UI Reset
    document.getElementById('quizResultWrapper').classList.add('hidden');
    document.getElementById('quizResultArea').classList.add('hidden');
    document.getElementById('quizRecordStatus').textContent = 'Đang ghi âm...';
    document.getElementById('quizRecordBtn').textContent = '⏹️';
    document.getElementById('quizRecordBtn').style.background = 'var(--accent-pink)';
    document.getElementById('quizRecordBtn').classList.add('recording-pulse');

    try {
        // Get fresh stream for each recording session
        stream = await navigator.mediaDevices.getUserMedia({
            audio: {
                echoCancellation: true,
                noiseSuppression: true,
                autoGainControl: true
            }
        });

        // Setup AudioContext - must resume on user gesture
        if (!audioContext) {
            audioContext = new (window.AudioContext || window.webkitAudioContext)();
        }

        // Resume AudioContext if suspended (required by browsers)
        if (audioContext.state === 'suspended') {
            await audioContext.resume();
        }

        analyser = audioContext.createAnalyser();
        const source = audioContext.createMediaStreamSource(stream);
        source.connect(analyser);
        analyser.fftSize = 256;

        // Start visualizer
        isRecording = true; // Set flag BEFORE starting visualizer
        drawVisualizer();

        // Setup Recorder with proper MIME type
        const mimeType = MediaRecorder.isTypeSupported('audio/webm') ? 'audio/webm' : 'audio/mp4';
        mediaRecorder = new MediaRecorder(stream, { mimeType });
        audioChunks = [];

        mediaRecorder.ondataavailable = event => {
            if (event.data.size > 0) audioChunks.push(event.data);
        };

        // Use timeslice to get data periodically
        mediaRecorder.start(100);
        console.log('Recording started with mimeType:', mimeType);

    } catch (err) {
        console.error('Microphone error:', err);
        alert('Không thể truy cập microphone: ' + err.message);
        resetRecordingUI();
    }
}

// Store recorded blob for playback and submission
let recordedBlob = null;
let recordedMimeType = 'audio/webm';

async function stopQuizRecordingAndSubmit() {
    // Redirect to the new stop function
    await stopQuizRecording();
}

async function stopQuizRecording() {
    if (!isRecording || !mediaRecorder) return;

    // Pause timer
    clearInterval(quizTimer);

    // UI Update - show we've stopped but not submitted yet
    document.getElementById('quizRecordStatus').textContent = 'Đã dừng ghi âm. Kiểm tra và nộp bài.';
    document.getElementById('quizRecordBtn').textContent = '🎤';
    document.getElementById('quizRecordBtn').style.background = '';
    document.getElementById('quizRecordBtn').classList.remove('recording-pulse');

    isRecording = false;

    // Stop Visualizer
    if (visualizerFrame) {
        cancelAnimationFrame(visualizerFrame);
        visualizerFrame = null;
    }

    return new Promise((resolve) => {
        mediaRecorder.onstop = async () => {
            if (stream) stream.getTracks().forEach(track => track.stop());

            recordedMimeType = mediaRecorder.mimeType || 'audio/webm';
            recordedBlob = new Blob(audioChunks, { type: recordedMimeType });
            console.log('Audio blob size:', recordedBlob.size, 'type:', recordedMimeType);

            if (recordedBlob.size < 1000) {
                document.getElementById('quizRecordStatus').textContent = 'Không ghi được âm thanh. Thử lại.';
                document.getElementById('recordingActions').classList.add('hidden');
                resolve();
                return;
            }

            // Set up audio element for playback
            const audioUrl = URL.createObjectURL(recordedBlob);
            document.getElementById('recordingPlayback').src = audioUrl;

            // Show action buttons
            document.getElementById('recordingActions').classList.remove('hidden');
            document.getElementById('recordingActions').style.display = 'flex';

            resolve();
        };

        mediaRecorder.stop();
    });
}

function playRecording() {
    const audio = document.getElementById('recordingPlayback');
    if (audio.src) {
        audio.currentTime = 0;
        audio.play();
    }
}

async function submitRecording() {
    if (!recordedBlob || recordedBlob.size < 1000) {
        alert('Không có ghi âm để nộp. Vui lòng ghi âm lại.');
        return;
    }

    document.getElementById('recordingActions').classList.add('hidden');
    document.getElementById('quizRecordStatus').textContent = 'Đang phân tích...';
    document.getElementById('quizRecordBtn').disabled = true;

    const formData = new FormData();
    const extension = recordedMimeType.includes('webm') ? '.webm' : '.m4a';
    formData.append('audio', recordedBlob, `recording${extension}`);
    formData.append('language', document.getElementById('languageSelect').value);

    try {
        const transRes = await fetch('/Practice/Transcribe', {
            method: 'POST',
            body: formData
        });

        if (!transRes.ok) throw new Error('Transcription failed');

        const transData = await transRes.json();
        const spokenText = transData.text || '';
        console.log('Transcribed text:', spokenText);

        checkQuizAnswer(spokenText);

    } catch (err) {
        console.error('Quiz processing error:', err);
        document.getElementById('quizRecordStatus').textContent = 'Lỗi xử lý. Thử lại.';
        resetRecordingUI();
    }
}



function resetRecordingUI() {
    isRecording = false;
    document.getElementById('quizRecordBtn').textContent = '🎤';
    document.getElementById('quizRecordBtn').style.background = '';
    document.getElementById('quizRecordBtn').disabled = false;
    document.getElementById('quizRecordStatus').textContent = 'Nhấn để bắt đầu nói';
    // Clear canvas
    const canvas = document.getElementById('quizVisualizer');
    const ctx = canvas.getContext('2d');
    ctx.clearRect(0, 0, canvas.width, canvas.height);
}

function drawVisualizer() {
    const canvas = document.getElementById('quizVisualizer');
    const ctx = canvas.getContext('2d');
    const bufferLength = analyser.frequencyBinCount;
    const dataArray = new Uint8Array(bufferLength);

    const draw = () => {
        if (!isRecording) return;
        visualizerFrame = requestAnimationFrame(draw);
        analyser.getByteTimeDomainData(dataArray);

        ctx.fillStyle = 'rgba(0, 0, 0, 0)'; // Transparent background
        ctx.clearRect(0, 0, canvas.width, canvas.height);

        ctx.lineWidth = 2;
        ctx.strokeStyle = '#a855f7'; // Purple accent
        ctx.beginPath();

        const sliceWidth = canvas.width * 1.0 / bufferLength;
        let x = 0;

        for (let i = 0; i < bufferLength; i++) {
            const v = dataArray[i] / 128.0;
            const y = v * canvas.height / 2;

            if (i === 0) ctx.moveTo(x, y);
            else ctx.lineTo(x, y);

            x += sliceWidth;
        }

        ctx.lineTo(canvas.width, canvas.height / 2);
        ctx.stroke();
    };

    draw();
}

async function checkQuizAnswer(spokenText) {
    clearInterval(quizTimer);
    resetRecordingUI(); // Reset button to mic icon

    const word = vocabularyList[quizIndex];
    const targetWordList = word.word.split(/[\/|;,\\=]/).map(s => s.trim());

    // Find best match
    let targetForEval = targetWordList[0];
    let minDist = 999;

    if (spokenText) {
        targetWordList.forEach(t => {
            const dist = levenshteinDistance(t.toLowerCase(), spokenText.toLowerCase());
            if (dist < minDist) {
                minDist = dist;
                targetForEval = t;
            }
        });
    }

    // Show Result UI
    const resultWrapper = document.getElementById('quizResultWrapper');
    resultWrapper.classList.remove('hidden');
    document.getElementById('quizUserAnswer').textContent = spokenText || '(Không nghe rõ)';

    // Evaluate
    try {
        const evalRes = await fetch('/Practice/EvaluatePronunciation', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                language: document.getElementById('languageSelect').value,
                targetText: targetForEval,
                spokenText: spokenText
            })
        });

        const result = await evalRes.json();
        const isCorrect = result.score >= 70;

        if (isCorrect) {
            quizCorrect++;
            document.getElementById('quizResultIcon').textContent = '✅';
            document.getElementById('quizResultText').textContent = 'Xuất sắc!';
            document.getElementById('quizResultText').style.color = 'var(--accent-green)';
        } else {
            quizWrong++;
            document.getElementById('quizResultIcon').textContent = '❌';
            document.getElementById('quizResultText').textContent = 'Cần cố gắng';
            document.getElementById('quizResultText').style.color = 'var(--accent-pink)';
        }

        document.getElementById('quizCorrectAnswer').innerHTML = `
            <div style="font-size: 0.95rem;">
                <p><strong>Điểm số:</strong> <span style="font-size: 1.2em; color: ${isCorrect ? 'var(--accent-green)' : 'var(--accent-pink)'}">${result.score}%</span></p>
                <div style="margin-top: 8px;"><strong>Chi tiết:</strong> ${result.feedback || 'Không có nhận xét'}</div>
                <div style="margin-top: 8px; color: var(--text-secondary);">Gợi ý: ${targetForEval}</div>
            </div>
        `;

        // Show/Hide Retry button
        if (quizRetriesLeft > 0) {
            document.getElementById('quizRetryBtn').classList.remove('hidden');
        } else {
            document.getElementById('quizRetryBtn').classList.add('hidden');
        }

    } catch (err) {
        console.error('Evaluation error:', err);
        // Fallback
        const fallbackCorrect = targetWordList.some(t =>
            t.toLowerCase() === spokenText.toLowerCase() ||
            levenshteinDistance(t.toLowerCase(), spokenText.toLowerCase()) <= 2
        );

        if (fallbackCorrect) {
            quizCorrect++;
            document.getElementById('quizResultIcon').textContent = '✅';
            document.getElementById('quizResultText').textContent = 'Đúng';
        } else {
            quizWrong++;
            document.getElementById('quizResultIcon').textContent = '❌';
            document.getElementById('quizResultText').textContent = 'Sai';
        }
        document.getElementById('quizCorrectAnswer').innerHTML = `<p>Đáp án: ${word.word}</p>`;
    }

    document.getElementById('quizResultArea').classList.remove('hidden');
}

function nextQuizQuestion() {
    quizIndex++;
    showQuizQuestion();
}

function skipQuestion() {
    clearInterval(quizTimer);
    quizWrong++;
    quizIndex++;
    showQuizQuestion();
}

function showHint() {
    document.getElementById('quizHint').classList.remove('hidden');
}

function showQuizSummary() {
    clearInterval(quizTimer);

    document.getElementById('correctCount').textContent = quizCorrect;
    document.getElementById('wrongCount').textContent = quizWrong;
    document.getElementById('quizScoreSummary').classList.remove('hidden');

    // Hide question UI
    document.getElementById('quizResultArea').classList.add('hidden');
}

function restartQuiz() {
    startQuiz();
}

// Levenshtein distance for fuzzy matching
function levenshteinDistance(a, b) {
    if (a.length === 0) return b.length;
    if (b.length === 0) return a.length;

    const matrix = [];
    for (let i = 0; i <= b.length; i++) {
        matrix[i] = [i];
    }
    for (let j = 0; j <= a.length; j++) {
        matrix[0][j] = j;
    }

    for (let i = 1; i <= b.length; i++) {
        for (let j = 1; j <= a.length; j++) {
            if (b.charAt(i - 1) === a.charAt(j - 1)) {
                matrix[i][j] = matrix[i - 1][j - 1];
            } else {
                matrix[i][j] = Math.min(
                    matrix[i - 1][j - 1] + 1,
                    matrix[i][j - 1] + 1,
                    matrix[i - 1][j] + 1
                );
            }
        }
    }
    return matrix[b.length][a.length];
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// ==================== PRACTICE MODE ====================

async function startPractice() {
    if (vocabularyList.length === 0) {
        alert("Vui lòng tải từ vựng trước.");
        return;
    }

    const level = document.getElementById('practiceLevel').value;
    const type = document.getElementById('practiceType').value;
    const language = document.getElementById('languageSelect').value;

    document.getElementById('practiceSettings').classList.add('hidden');
    document.getElementById('practiceLoading').classList.remove('hidden');
    document.getElementById('practiceQuiz').classList.add('hidden');
    document.getElementById('practiceSummary').classList.add('hidden');

    try {
        const wordList = vocabularyList.map(v => v.word);

        const response = await fetch('/Practice/GeneratePractice', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                words: wordList,
                language: language,
                level: level,
                type: type,
                count: 5
            })
        });

        if (!response.ok) throw new Error('Failed to generate practice');

        const data = await response.json();
        practiceExercises = data.exercises || [];
        practiceIndex = 0;
        practiceScore = 0;
        currentPracticeType = type;

        if (practiceExercises.length === 0) {
            alert("Không thể tạo bài tập. Vui lòng thử lại.");
            resetPractice();
        } else {
            document.getElementById('practiceLoading').classList.add('hidden');
            showPracticeQuestion();
        }

    } catch (error) {
        console.error('Practice generation error:', error);
        alert('Lỗi tạo bài tập.');
        resetPractice();
    }
}

function showPracticeQuestion() {
    if (practiceIndex >= practiceExercises.length) {
        showPracticeSummary();
        return;
    }

    const exercise = practiceExercises[practiceIndex];
    document.getElementById('practiceQuiz').classList.remove('hidden');
    document.getElementById('practiceSettings').classList.add('hidden');

    document.getElementById('practiceCurrentIndex').textContent = practiceIndex + 1;
    document.getElementById('practiceTotalCount').textContent = practiceExercises.length;
    document.getElementById('practiceScore').textContent = practiceScore;
    document.getElementById('practiceQuestion').textContent = exercise.question;

    // Reset Input Areas
    document.getElementById('practiceOptions').classList.add('hidden');
    document.getElementById('practiceArrange').classList.add('hidden');
    document.getElementById('practiceTranslate').classList.add('hidden');
    document.getElementById('practiceFeedback').classList.add('hidden');
    document.getElementById('practiceCheckBtn').classList.remove('hidden');
    document.getElementById('practiceNextBtn').classList.add('hidden');

    // Setup input based on type
    if (currentPracticeType === 'fill_blank') {
        const optionsDiv = document.getElementById('practiceOptions');
        optionsDiv.classList.remove('hidden');
        optionsDiv.innerHTML = exercise.options.map((opt, idx) => `
            <button class="btn btn-secondary practice-option" onclick="selectPracticeOption(this, '${escapeHtml(opt)}')">${escapeHtml(opt)}</button>
        `).join('');
    } else if (currentPracticeType === 'arrange') {
        document.getElementById('practiceArrange').classList.remove('hidden');
        const sourceDiv = document.getElementById('practiceArrangeSource');
        const targetDiv = document.getElementById('practiceArrangeTarget');
        sourceDiv.innerHTML = '';
        targetDiv.innerHTML = '';
        originalArrangeSource = [...exercise.options];

        exercise.options.forEach(word => {
            const btn = document.createElement('button');
            btn.className = 'btn btn-sm btn-outline-secondary';
            btn.textContent = word;
            btn.onclick = () => moveWordToTarget(btn);
            sourceDiv.appendChild(btn);
        });
    } else if (currentPracticeType === 'translate') {
        document.getElementById('practiceTranslate').classList.remove('hidden');
        document.getElementById('practiceTranslateInput').value = '';
    }
}

let selectedOptionBtn = null;
let selectedAnswer = '';

function selectPracticeOption(btn, value) {
    if (selectedOptionBtn) selectedOptionBtn.classList.remove('btn-primary');
    selectedOptionBtn = btn;
    selectedAnswer = value;
    btn.classList.add('btn-primary');
}

function moveWordToTarget(btn) {
    const targetDiv = document.getElementById('practiceArrangeTarget');
    targetDiv.appendChild(btn);
    btn.onclick = () => moveWordToSource(btn);
    btn.classList.remove('btn-outline-secondary');
    btn.classList.add('btn-primary');
}

function moveWordToSource(btn) {
    const sourceDiv = document.getElementById('practiceArrangeSource');
    sourceDiv.appendChild(btn);
    btn.onclick = () => moveWordToTarget(btn);
    btn.classList.remove('btn-primary');
    btn.classList.add('btn-outline-secondary');
}

function getArrangeAnswer() {
    const targetDiv = document.getElementById('practiceArrangeTarget');
    const words = Array.from(targetDiv.children).map(btn => btn.textContent);
    return words.join(' ');
}

async function checkPracticeAnswer() {
    let userAnswer = '';

    if (currentPracticeType === 'fill_blank') {
        if (!selectedAnswer) {
            alert('Vui lòng chọn đáp án!');
            return;
        }
        userAnswer = selectedAnswer;
    } else if (currentPracticeType === 'arrange') {
        userAnswer = getArrangeAnswer();
        if (!userAnswer) {
            alert('Vui lòng sắp xếp từ!');
            return;
        }
    } else if (currentPracticeType === 'translate') {
        userAnswer = document.getElementById('practiceTranslateInput').value.trim();
        if (!userAnswer) {
            alert('Vui lòng nhập câu dịch!');
            return;
        }
    }

    const exercise = practiceExercises[practiceIndex];
    let isCorrect = false;
    let aiResponse = null;

    // For translation, use AI checking
    if (currentPracticeType === 'translate') {
        // Show loading state
        const checkBtn = document.getElementById('practiceCheckBtn');
        checkBtn.disabled = true;
        checkBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Đang kiểm tra...';

        try {
            const language = document.getElementById('languageSelect').value;
            const response = await fetch('/Practice/CheckTranslation', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    originalText: exercise.question,
                    userAnswer: userAnswer,
                    language: language,
                    expectedAnswer: exercise.correctAnswer
                })
            });

            if (response.ok) {
                aiResponse = await response.json();
                isCorrect = aiResponse.isCorrect;
            } else {
                console.error('AI check failed');
                // Fallback to string comparison
                const normUser = userAnswer.toLowerCase().trim();
                const normCorrect = exercise.correctAnswer.toLowerCase().trim();
                isCorrect = normUser === normCorrect;
            }
        } catch (err) {
            console.error('Error checking translation:', err);
            // Fallback to string comparison
            const normUser = userAnswer.toLowerCase().trim();
            const normCorrect = exercise.correctAnswer.toLowerCase().trim();
            isCorrect = normUser === normCorrect;
        }

        checkBtn.disabled = false;
        checkBtn.innerHTML = 'Kiểm tra';
    } else {
        // For other types, use string comparison
        const normUser = userAnswer.toLowerCase().replace(/[.,\/#!$%\^&\*;:{}=\-_`~()]/g, "").trim();
        const normCorrect = exercise.correctAnswer.toLowerCase().replace(/[.,\/#!$%\^&\*;:{}=\-_`~()]/g, "").trim();
        isCorrect = normUser === normCorrect;
    }

    // Show Feedback
    const feedbackDiv = document.getElementById('practiceFeedback');
    feedbackDiv.classList.remove('hidden');
    const feedbackText = document.getElementById('practiceFeedbackText');
    const explanation = document.getElementById('practiceExplanation');

    // Build detailed feedback HTML
    let feedbackHTML = '';

    // Show user's answer
    feedbackHTML += `<div style="background: rgba(0,0,0,0.2); padding: 12px; border-radius: 8px; margin-bottom: 12px;">`;
    feedbackHTML += `<p style="color: var(--text-muted); font-size: 0.9rem; margin-bottom: 4px;">Câu trả lời của bạn:</p>`;
    feedbackHTML += `<p style="font-size: 1.1rem; font-weight: 600; color: ${isCorrect ? 'var(--accent-green)' : 'var(--accent-pink)'};">${escapeHtml(userAnswer)}</p>`;
    feedbackHTML += `</div>`;

    // Show correct answer / AI feedback
    if (aiResponse) {
        // AI-based feedback for translation
        if (aiResponse.score >= 0) {
            feedbackHTML += `<div style="background: rgba(124, 58, 237, 0.1); padding: 12px; border-radius: 8px; margin-bottom: 12px; border-left: 3px solid var(--primary-light);">`;
            feedbackHTML += `<p style="color: var(--text-muted); font-size: 0.9rem; margin-bottom: 4px;">📊 Điểm: <strong>${aiResponse.score}/100</strong></p>`;
            feedbackHTML += `</div>`;
        }

        feedbackHTML += `<div style="background: rgba(0,255,0,0.1); padding: 12px; border-radius: 8px; margin-bottom: 12px; border-left: 3px solid var(--accent-green);">`;
        feedbackHTML += `<p style="color: var(--text-muted); font-size: 0.9rem; margin-bottom: 4px;">✅ Bản dịch chuẩn:</p>`;
        feedbackHTML += `<p style="font-size: 1.2rem; font-weight: 700; color: var(--accent-green);">${escapeHtml(aiResponse.correctedTranslation || exercise.correctAnswer)}</p>`;
        feedbackHTML += `</div>`;

        if (aiResponse.feedback) {
            feedbackHTML += `<div style="background: rgba(124, 58, 237, 0.1); padding: 12px; border-radius: 8px; margin-bottom: 12px;">`;
            feedbackHTML += `<p style="color: var(--text-muted); font-size: 0.9rem; margin-bottom: 4px;">💬 Nhận xét:</p>`;
            feedbackHTML += `<p style="line-height: 1.5;">${escapeHtml(aiResponse.feedback)}</p>`;
            feedbackHTML += `</div>`;
        }

        if (aiResponse.wordByWordBreakdown) {
            feedbackHTML += `<div style="background: rgba(59, 130, 246, 0.1); padding: 12px; border-radius: 8px; margin-bottom: 12px; border-left: 3px solid #3b82f6;">`;
            feedbackHTML += `<p style="color: var(--text-muted); font-size: 0.9rem; margin-bottom: 4px;">📝 Phân tích từng từ:</p>`;
            feedbackHTML += `<p style="line-height: 1.6; white-space: pre-wrap;">${escapeHtml(aiResponse.wordByWordBreakdown)}</p>`;
            feedbackHTML += `</div>`;
        }

        if (aiResponse.grammarNotes) {
            feedbackHTML += `<div style="background: rgba(234, 179, 8, 0.1); padding: 12px; border-radius: 8px; margin-bottom: 12px; border-left: 3px solid #eab308;">`;
            feedbackHTML += `<p style="color: var(--text-muted); font-size: 0.9rem; margin-bottom: 4px;">📖 Ngữ pháp:</p>`;
            feedbackHTML += `<p style="line-height: 1.5;">${escapeHtml(aiResponse.grammarNotes)}</p>`;
            feedbackHTML += `</div>`;
        }

        if (aiResponse.alternativeTranslations && aiResponse.alternativeTranslations.length > 0) {
            feedbackHTML += `<div style="background: rgba(16, 185, 129, 0.1); padding: 12px; border-radius: 8px; border-left: 3px solid #10b981;">`;
            feedbackHTML += `<p style="color: var(--text-muted); font-size: 0.9rem; margin-bottom: 4px;">🔄 Cách dịch khác:</p>`;
            aiResponse.alternativeTranslations.forEach(alt => {
                feedbackHTML += `<p style="margin-bottom: 4px;">• ${escapeHtml(alt)}</p>`;
            });
            feedbackHTML += `</div>`;
        }
    } else {
        // Standard feedback for non-translate types
        feedbackHTML += `<div style="background: rgba(0,255,0,0.1); padding: 12px; border-radius: 8px; margin-bottom: 12px; border-left: 3px solid var(--accent-green);">`;
        feedbackHTML += `<p style="color: var(--text-muted); font-size: 0.9rem; margin-bottom: 4px;">✅ Đáp án đúng:</p>`;
        feedbackHTML += `<p style="font-size: 1.2rem; font-weight: 700; color: var(--accent-green);">${escapeHtml(exercise.correctAnswer)}</p>`;
        feedbackHTML += `</div>`;

        // Show bilingual explanation
        if (exercise.explanation) {
            feedbackHTML += `<div style="background: rgba(124, 58, 237, 0.1); padding: 12px; border-radius: 8px; border-left: 3px solid var(--primary-light);">`;
            feedbackHTML += `<p style="color: var(--text-muted); font-size: 0.9rem; margin-bottom: 8px;">📖 Giải thích:</p>`;

            const explanationLines = exercise.explanation.split('\n').filter(line => line.trim());
            explanationLines.forEach(line => {
                feedbackHTML += `<p style="margin-bottom: 6px; line-height: 1.5;">${escapeHtml(line)}</p>`;
            });

            if (exercise.explanationVi && !exercise.explanation.includes(exercise.explanationVi)) {
                feedbackHTML += `<p style="margin-top: 8px; color: var(--text-secondary);">🇻🇳 ${escapeHtml(exercise.explanationVi)}</p>`;
            }

            feedbackHTML += `</div>`;
        }
    }

    if (isCorrect) {
        practiceScore++;
        feedbackText.textContent = '✅ Chính xác!';
        feedbackText.style.color = 'var(--accent-green)';
        feedbackDiv.style.background = 'rgba(34, 197, 94, 0.1)';
    } else {
        feedbackText.textContent = '❌ Chưa đúng';
        feedbackText.style.color = 'var(--accent-pink)';
        feedbackDiv.style.background = 'rgba(236, 72, 153, 0.1)';
    }
    document.getElementById('practiceScore').textContent = practiceScore;
    explanation.innerHTML = feedbackHTML;

    document.getElementById('practiceCheckBtn').classList.add('hidden');
    document.getElementById('practiceNextBtn').classList.remove('hidden');
}

function nextPracticeQuestion() {
    practiceIndex++;
    selectedOptionBtn = null;
    selectedAnswer = '';
    showPracticeQuestion();
}

function showPracticeSummary() {
    document.getElementById('practiceQuiz').classList.add('hidden');
    document.getElementById('practiceSummary').classList.remove('hidden');
    document.getElementById('practiceSettings').classList.add('hidden');
    document.getElementById('practiceFinalScore').textContent = `${practiceScore} / ${practiceExercises.length}`;
}

function resetPractice() {
    document.getElementById('practiceSummary').classList.add('hidden');
    document.getElementById('practiceSettings').classList.remove('hidden');
    document.getElementById('practiceLoading').classList.add('hidden'); // Ensure loading is hidden
}

