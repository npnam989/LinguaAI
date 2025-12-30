// Audio Noise Gate using Web Audio API
// Optimized version using lightweight processing

class AudioNoiseGate {
    constructor() {
        this.audioContext = null;
        this.analyser = null;
        this.mediaStream = null;
        this.isActive = false;
        this.threshold = 15; // Volume threshold (0-255 scale)
        this.volumeCallback = null;
        this.animationId = null;
    }

    async init() {
        try {
            // Get microphone access
            this.mediaStream = await navigator.mediaDevices.getUserMedia({
                audio: {
                    echoCancellation: true,
                    noiseSuppression: true,  // Browser's built-in noise suppression
                    autoGainControl: true
                }
            });

            // Create audio context
            this.audioContext = new (window.AudioContext || window.webkitAudioContext)();
            const source = this.audioContext.createMediaStreamSource(this.mediaStream);

            // Create analyser with small FFT for speed
            this.analyser = this.audioContext.createAnalyser();
            this.analyser.fftSize = 256; // Smaller = faster (256 is minimum)
            this.analyser.smoothingTimeConstant = 0.3; // Faster response

            source.connect(this.analyser);
            this.isActive = true;

            return true;
        } catch (error) {
            console.error('Failed to initialize audio:', error);
            return false;
        }
    }

    // Get current volume level (0-100)
    getVolume() {
        if (!this.analyser) return 0;

        const dataArray = new Uint8Array(this.analyser.frequencyBinCount);
        this.analyser.getByteFrequencyData(dataArray);

        // Quick average of first 32 bins (voice frequency range)
        let sum = 0;
        for (let i = 0; i < 32; i++) {
            sum += dataArray[i];
        }
        return Math.round(sum / 32);
    }

    // Check if current audio is above noise threshold
    isVoiceDetected() {
        return this.getVolume() > this.threshold;
    }

    // Calibrate noise floor from ambient sound
    async calibrateNoiseFloor(durationMs = 1000) {
        return new Promise((resolve) => {
            const samples = [];
            const startTime = Date.now();

            const measure = () => {
                if (Date.now() - startTime < durationMs) {
                    samples.push(this.getVolume());
                    requestAnimationFrame(measure);
                } else {
                    // Set threshold slightly above average noise
                    const avgNoise = samples.reduce((a, b) => a + b, 0) / samples.length;
                    this.threshold = Math.round(avgNoise * 1.5 + 5);
                    console.log(`Noise calibrated: avg=${avgNoise.toFixed(1)}, threshold=${this.threshold}`);
                    resolve(this.threshold);
                }
            };
            measure();
        });
    }

    // Start volume monitoring with callback
    startMonitoring(callback) {
        this.volumeCallback = callback;
        const monitor = () => {
            if (this.isActive && this.volumeCallback) {
                const volume = this.getVolume();
                const isVoice = volume > this.threshold;
                this.volumeCallback(volume, isVoice, this.threshold);
                this.animationId = requestAnimationFrame(monitor);
            }
        };
        monitor();
    }

    stopMonitoring() {
        if (this.animationId) {
            cancelAnimationFrame(this.animationId);
            this.animationId = null;
        }
    }

    // Set sensitivity (lower = more sensitive)
    setSensitivity(value) {
        this.threshold = Math.max(5, Math.min(100, value));
    }

    // Cleanup
    destroy() {
        this.isActive = false;
        this.stopMonitoring();

        if (this.mediaStream) {
            this.mediaStream.getTracks().forEach(track => track.stop());
        }
        if (this.audioContext) {
            this.audioContext.close();
        }
    }
}

// Global instance
window.audioNoiseGate = null;

// Initialize noise gate
async function initNoiseGate() {
    if (!window.audioNoiseGate) {
        window.audioNoiseGate = new AudioNoiseGate();
        await window.audioNoiseGate.init();
    }
    return window.audioNoiseGate;
}
