document.addEventListener('DOMContentLoaded', function() {
    const form = document.getElementById('editJobCodeForm');
    const saveButton = document.getElementById('saveButton');

    if (form) {
        form.addEventListener('submit', async function(e) {
            e.preventDefault();

            if (!form.checkValidity()) {
                e.stopPropagation();
                form.classList.add('was-validated');
                return;
            }

            saveButton.disabled = true;
            saveButton.innerHTML = '<i class="ti ti-loader me-2"></i>Saving...';

            const formData = {
                idJobCode: parseInt(document.getElementById('idJobCode').value),
                name: document.getElementById('name').value,
                group: document.getElementById('group').value,
                description: document.getElementById('description').value || null,
                laborOrMaterial: document.querySelector('input[name="laborOrMaterial"]:checked').value,
                estimationTimeDays: parseInt(document.getElementById('estimationTimeDays').value) || 0,
                estimationTimeHours: parseInt(document.getElementById('estimationTimeHours').value) || 0,
                estimationTimeMinutes: parseInt(document.getElementById('estimationTimeMinutes').value) || 0,
                currency: document.getElementById('currency').value,
                unitPrice: parseFloat(document.getElementById('unitPrice').value),
                measurementUnit: document.getElementById('measurementUnit').value,
                minimumStock: parseFloat(document.getElementById('minimumStock').value) || 0
            };

            try {
                const response = await fetch('/jobcode/edit', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify(formData)
                });

                const result = await response.json();

                if (result.success) {
                    showNotification('Job code updated successfully', 'success', 'Success');
                    setTimeout(function() {
                        window.location.href = '/jobcode/detail/' + formData.idJobCode;
                    }, 1500);
                } else {
                    showNotification(result.message || 'Failed to update job code', 'error', 'Error');
                    saveButton.disabled = false;
                    saveButton.innerHTML = '<i class="ti ti-device-floppy me-2"></i>Save Changes';
                }
            } catch (error) {
                console.error('Error updating job code:', error);
                showNotification('An error occurred while updating the job code', 'error', 'Error');
                saveButton.disabled = false;
                saveButton.innerHTML = '<i class="ti ti-device-floppy me-2"></i>Save Changes';
            }
        });

        const hourInput = document.getElementById('estimationTimeHours');
        const minuteInput = document.getElementById('estimationTimeMinutes');

        if (hourInput) {
            hourInput.addEventListener('input', function() {
                if (this.value > 23) this.value = 23;
                if (this.value < 0) this.value = 0;
            });
        }

        if (minuteInput) {
            minuteInput.addEventListener('input', function() {
                if (this.value > 59) this.value = 59;
                if (this.value < 0) this.value = 0;
            });
        }

        const numericInputs = document.querySelectorAll('input[type="number"]');
        numericInputs.forEach(input => {
            input.addEventListener('input', function() {
                if (this.value < 0) this.value = 0;
            });
        });
    }
});
