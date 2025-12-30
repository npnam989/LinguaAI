// Vocabulary Mode JavaScript
let vocabularyList = [];
let currentIndex = 0;
let vocabularySource = 'ai'; // 'ai' or 'file'
let currentMode = 'flashcard'; // 'flashcard' or 'quiz'

// Quiz state
let quizIndex = 0;
let quizCorrect = 0;
let quizWrong = 0;
let quizTimer = null;
let quizTimeLeft = 10;
let quizRecognition = null;
const QUIZ_TIME_LIMIT = 10; // seconds

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
function initQuizSpeechRecognition() {
    if ('webkitSpeechRecognition' in window || 'SpeechRecognition' in window) {
        const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
        quizRecognition = new SpeechRecognition();

        // Optimized settings for faster recognition
        quizRecognition.continuous = false;
        quizRecognition.interimResults = true; // Enable interim results for faster feedback
        quizRecognition.maxAlternatives = 3;   // Get multiple alternatives for better matching

        let hasProcessed = false; // Prevent duplicate processing

        quizRecognition.onresult = (event) => {
            const result = event.results[0];

            // Show interim result in real-time
            const transcript = result[0].transcript;
            document.getElementById('quizRecordStatus').textContent = `"${transcript}"`;

            // Process final result OR confident interim result
            if (result.isFinal || result[0].confidence > 0.7) {
                if (!hasProcessed) {
                    hasProcessed = true;
                    stopQuizRecording();

                    // Try all alternatives for better matching
                    let bestMatch = transcript;
                    const correctWord = vocabularyList[quizIndex]?.word?.toLowerCase();

                    for (let i = 0; i < result.length; i++) {
                        const alt = result[i].transcript.toLowerCase().trim();
                        if (alt === correctWord || alt.includes(correctWord) || correctWord.includes(alt)) {
                            bestMatch = result[i].transcript;
                            break;
                        }
                    }

                    checkQuizAnswer(bestMatch);
                }
            }
        };

        quizRecognition.onerror = (event) => {
            console.error('Speech recognition error:', event.error);
            if (event.error === 'no-speech') {
                document.getElementById('quizRecordStatus').textContent = 'Không nghe thấy. Nói gần mic hơn.';
            } else if (event.error === 'audio-capture') {
                document.getElementById('quizRecordStatus').textContent = 'Không tìm thấy microphone.';
            } else {
                stopQuizRecording();
                document.getElementById('quizRecordStatus').textContent = 'Lỗi. Thử lại hoặc bỏ qua.';
            }
        };

        quizRecognition.onend = () => {
            if (!hasProcessed) {
                stopQuizRecording();
            }
        };

        quizRecognition.onaudiostart = () => {
            hasProcessed = false;
            document.getElementById('quizRecordStatus').textContent = 'Đang nghe...';
        };
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
        document.getElementById('quizDisplayLangSelector').classList.add('hidden');
    } else {
        document.getElementById('flashcardArea').classList.add('hidden');
        document.getElementById('quizArea').classList.remove('hidden');
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

    document.getElementById('quizAnswerArea').classList.add('hidden');
    document.getElementById('quizResultArea').classList.add('hidden');
    document.getElementById('quizRecordBtn').disabled = false;
    document.getElementById('quizRecordStatus').textContent = 'Nhấn để trả lời';

    // Start timer
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
    quizWrong++;

    const word = vocabularyList[quizIndex];
    document.getElementById('quizResultIcon').textContent = '⏰';
    document.getElementById('quizResultText').textContent = 'Hết giờ!';
    document.getElementById('quizResultText').style.color = 'var(--accent-pink)';
    document.getElementById('quizCorrectAnswer').textContent = `Đáp án: ${word.word} (${word.pronunciation || ''})`;
    document.getElementById('quizResultArea').classList.remove('hidden');
    document.getElementById('quizRecordBtn').disabled = true;
}

async function startQuizRecording() {
    if (!quizRecognition) {
        alert('Trình duyệt không hỗ trợ nhận dạng giọng nói');
        return;
    }

    // Initialize noise gate if available
    if (typeof initNoiseGate === 'function' && !window.audioNoiseGate) {
        try {
            await initNoiseGate();
            // Start volume monitoring
            window.audioNoiseGate.startMonitoring((volume, isVoice, threshold) => {
                const meter = document.getElementById('volumeMeter');
                if (meter) {
                    meter.style.width = Math.min(volume, 100) + '%';
                    meter.style.background = isVoice ? 'var(--accent-purple)' : 'var(--text-secondary)';
                }
            });
        } catch (e) {
            console.log('Noise gate not available:', e);
        }
    }

    const language = document.getElementById('languageSelect').value;
    const langMap = { 'ko': 'ko-KR', 'zh': 'zh-CN', 'en': 'en-US' };
    quizRecognition.lang = langMap[language] || 'en-US';

    try {
        quizRecognition.start();
        document.getElementById('quizRecordBtn').style.background = 'var(--accent-pink)';
        document.getElementById('quizRecordBtn').textContent = '🔴';
        document.getElementById('quizRecordStatus').textContent = 'Đang nghe...';
    } catch (e) {
        console.error('Error starting recognition:', e);
    }
}

function stopQuizRecording() {
    if (quizRecognition) {
        try {
            quizRecognition.stop();
        } catch (e) { }
    }

    // Stop volume monitoring
    if (window.audioNoiseGate) {
        window.audioNoiseGate.stopMonitoring();
    }

    document.getElementById('quizRecordBtn').style.background = '';
    document.getElementById('quizRecordBtn').textContent = '🎤';
}

function checkQuizAnswer(spokenText) {
    clearInterval(quizTimer);

    const word = vocabularyList[quizIndex];
    const correctWord = word.word.toLowerCase().trim();
    const userWord = spokenText.toLowerCase().trim();

    // Show user's answer
    document.getElementById('quizUserAnswer').textContent = spokenText;
    document.getElementById('quizAnswerArea').classList.remove('hidden');

    // Check if correct (exact match or close enough)
    const isCorrect = correctWord === userWord ||
        correctWord.includes(userWord) ||
        userWord.includes(correctWord) ||
        levenshteinDistance(correctWord, userWord) <= 2;

    if (isCorrect) {
        quizCorrect++;
        document.getElementById('quizResultIcon').textContent = '✅';
        document.getElementById('quizResultText').textContent = 'Chính xác!';
        document.getElementById('quizResultText').style.color = 'var(--accent-green)';
        document.getElementById('quizCorrectAnswer').textContent = '';
    } else {
        quizWrong++;
        document.getElementById('quizResultIcon').textContent = '❌';
        document.getElementById('quizResultText').textContent = 'Chưa đúng!';
        document.getElementById('quizResultText').style.color = 'var(--accent-pink)';
        document.getElementById('quizCorrectAnswer').textContent = `Đáp án: ${word.word} (${word.pronunciation || ''})`;
    }

    document.getElementById('quizResultArea').classList.remove('hidden');
    document.getElementById('quizRecordBtn').disabled = true;
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
    document.getElementById('quizAnswerArea').classList.add('hidden');
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
