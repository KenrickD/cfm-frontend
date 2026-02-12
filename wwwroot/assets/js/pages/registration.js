/**
 * Registration page JavaScript
 * Handles license key auto-focus and uppercase conversion
 */
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        initLicenseKeyInputs();
    });

    /**
     * Initialize license key input behavior
     * - Auto-focus to next input when 4 characters are entered
     * - Convert input to uppercase
     * - Handle backspace to go to previous input
     */
    function initLicenseKeyInputs() {
        const keyInputs = ['key1', 'key2', 'key3', 'key4'];

        keyInputs.forEach(function (inputId, index) {
            const input = document.getElementById(inputId);
            if (!input) return;

            // Convert to uppercase on input
            input.addEventListener('input', function (e) {
                this.value = this.value.toUpperCase();

                // Auto-focus to next input when 4 characters are entered
                if (this.value.length >= 4 && index < keyInputs.length - 1) {
                    const nextInput = document.getElementById(keyInputs[index + 1]);
                    if (nextInput) {
                        nextInput.focus();
                        nextInput.select();
                    }
                }
            });

            // Handle backspace to go to previous input
            input.addEventListener('keydown', function (e) {
                if (e.key === 'Backspace' && this.value.length === 0 && index > 0) {
                    const prevInput = document.getElementById(keyInputs[index - 1]);
                    if (prevInput) {
                        prevInput.focus();
                        // Set cursor at end
                        prevInput.selectionStart = prevInput.selectionEnd = prevInput.value.length;
                    }
                }
            });

            // Handle paste - distribute across inputs
            input.addEventListener('paste', function (e) {
                e.preventDefault();
                const pastedText = (e.clipboardData || window.clipboardData).getData('text');

                // Remove dashes and spaces, convert to uppercase
                const cleanText = pastedText.replace(/[-\s]/g, '').toUpperCase();

                // Distribute across inputs starting from current
                let remaining = cleanText;
                for (let i = index; i < keyInputs.length && remaining.length > 0; i++) {
                    const targetInput = document.getElementById(keyInputs[i]);
                    if (targetInput) {
                        targetInput.value = remaining.substring(0, 4);
                        remaining = remaining.substring(4);
                    }
                }

                // Focus on next empty input or last input
                for (let i = 0; i < keyInputs.length; i++) {
                    const targetInput = document.getElementById(keyInputs[i]);
                    if (targetInput && targetInput.value.length < 4) {
                        targetInput.focus();
                        break;
                    }
                    if (i === keyInputs.length - 1) {
                        targetInput.focus();
                    }
                }
            });
        });

        // Auto-focus first input
        const firstInput = document.getElementById('key1');
        if (firstInput) {
            firstInput.focus();
        }
    }
})();
