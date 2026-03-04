document.addEventListener('DOMContentLoaded', function() {
    const form = document.getElementById('editJobCodeForm');
    const updateButton = document.getElementById('updateButton');

    if (form) {
        form.addEventListener('submit', async function(e) {
            e.preventDefault();

            if (!form.checkValidity()) {
                e.stopPropagation();
                form.classList.add('was-validated');
                return;
            }

            updateButton.disabled = true;
            updateButton.innerHTML = '<i class="ti ti-loader me-2"></i>Updating...';

            const days = parseInt(document.getElementById('estimationTimeDays').value) || 0;
            const hours = parseInt(document.getElementById('estimationTimeHours').value) || 0;
            const minutes = parseInt(document.getElementById('estimationTimeMinutes').value) || 0;

            const materialTypeIdValue = document.getElementById('materialTypeId').value;
            const materialTypeId = materialTypeIdValue && materialTypeIdValue !== '' ? parseInt(materialTypeIdValue) : null;

            const formData = {
                IdJobCode: parseInt(document.getElementById('idJobCode').value),
                Name: document.getElementById('name').value,
                Group_IdType: parseInt(document.getElementById('group').value),
                Description: document.getElementById('description').value || null,
                Label_IdEnum: parseInt(document.querySelector('input[name="laborOrMaterial"]:checked').value),
                MaterialType_IdType: materialTypeId,
                Currency_IdEnum: parseInt(document.getElementById('currency').value),
                UnitPrice: parseFloat(document.getElementById('unitPrice').value),
                MeasurementUnit_IdEnum: parseInt(document.getElementById('measurementUnit').value),
                MinimumStock: parseFloat(document.getElementById('minimumStock').value) || null,
                EstimationTime: {
                    Days: days,
                    Hours: hours,
                    Minutes: minutes,
                    TimeSpan: (days * 24 * 60 * 60 * 1000) + (hours * 60 * 60 * 1000) + (minutes * 60 * 1000)
                }
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
                        window.location.href = '/jobcode/detail/' + formData.IdJobCode;
                    }, 1500);
                } else {
                    showNotification(result.message || 'Failed to update job code', 'error', 'Error');
                    updateButton.disabled = false;
                    updateButton.innerHTML = '<i class="ti ti-device-floppy me-2"></i>Update Job Code';
                }
            } catch (error) {
                console.error('Error updating job code:', error);
                showNotification('An error occurred while updating the job code', 'error', 'Error');
                updateButton.disabled = false;
                updateButton.innerHTML = '<i class="ti ti-device-floppy me-2"></i>Update Job Code';
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
