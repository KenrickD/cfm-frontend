/**
 * Add Job Code Page JavaScript
 * Handles form validation and submission for creating new job codes
 */

(function() {
    'use strict';

    const form = document.getElementById('addJobCodeForm');
    const saveButton = document.getElementById('saveButton');

    // Initialize form validation
    function initFormValidation() {
        form.addEventListener('submit', handleFormSubmit);
    }

    // Handle form submission
    async function handleFormSubmit(e) {
        e.preventDefault();

        if (!form.checkValidity()) {
            e.stopPropagation();
            form.classList.add('was-validated');
            return;
        }

        const formData = {
            name: document.getElementById('name').value.trim(),
            description: document.getElementById('description').value.trim() || null,
            group: document.getElementById('group').value,
            laborOrMaterial: document.querySelector('input[name="laborOrMaterial"]:checked').value,
            materialType: null,
            estimationTimeDays: parseInt(document.getElementById('estimationTimeDays').value) || 0,
            estimationTimeHours: parseInt(document.getElementById('estimationTimeHours').value) || 0,
            estimationTimeMinutes: parseInt(document.getElementById('estimationTimeMinutes').value) || 0,
            measurementUnit: document.getElementById('measurementUnit').value,
            unitPrice: parseFloat(document.getElementById('unitPrice').value) || 0,
            currency: document.getElementById('currency').value,
            minimumStock: parseFloat(document.getElementById('minimumStock').value) || 0
        };

        // Disable submit button
        saveButton.disabled = true;
        const originalText = saveButton.innerHTML;
        saveButton.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status"></span>Saving...';

        try {
            const response = await fetch('/JobCode/Add', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(formData)
            });

            const result = await response.json();

            if (result.success) {
                showNotification(result.message || 'Job code created successfully', 'success', 'Success');

                setTimeout(() => {
                    window.location.href = '/JobCode/Index';
                }, 1500);
            } else {
                showNotification(result.message || 'Failed to create job code', 'error', 'Error');
                saveButton.disabled = false;
                saveButton.innerHTML = originalText;
            }
        } catch (error) {
            console.error('Error creating job code:', error);
            showNotification('Network error. Please try again.', 'error', 'Error');
            saveButton.disabled = false;
            saveButton.innerHTML = originalText;
        }
    }

    // Validate estimation time inputs
    function validateTimeInputs() {
        const hoursInput = document.getElementById('estimationTimeHours');
        const minutesInput = document.getElementById('estimationTimeMinutes');

        hoursInput.addEventListener('change', function() {
            const value = parseInt(this.value);
            if (value > 23) this.value = 23;
            if (value < 0) this.value = 0;
        });

        minutesInput.addEventListener('change', function() {
            const value = parseInt(this.value);
            if (value > 59) this.value = 59;
            if (value < 0) this.value = 0;
        });
    }

    // Validate numeric inputs
    function validateNumericInputs() {
        const numericInputs = [
            document.getElementById('unitPrice'),
            document.getElementById('minimumStock'),
            document.getElementById('estimationTimeDays'),
            document.getElementById('estimationTimeHours'),
            document.getElementById('estimationTimeMinutes')
        ];

        numericInputs.forEach(input => {
            if (input) {
                input.addEventListener('input', function() {
                    const value = parseFloat(this.value);
                    if (value < 0) this.value = 0;
                });
            }
        });
    }

    // Remove invalid feedback on input
    function setupInputFeedback() {
        const inputs = form.querySelectorAll('.form-control, .form-select');

        inputs.forEach(input => {
            input.addEventListener('input', function() {
                if (this.checkValidity()) {
                    this.classList.remove('is-invalid');
                    this.classList.add('is-valid');
                } else {
                    this.classList.remove('is-valid');
                }
            });

            input.addEventListener('blur', function() {
                if (!this.checkValidity() && this.value !== '') {
                    this.classList.add('is-invalid');
                }
            });
        });
    }

    // Initialize on DOM ready
    document.addEventListener('DOMContentLoaded', function() {
        initFormValidation();
        validateTimeInputs();
        validateNumericInputs();
        setupInputFeedback();
    });

})();
