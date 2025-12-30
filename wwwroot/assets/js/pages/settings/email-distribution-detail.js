(function () {
    'use strict';

    const CONFIG = {
        apiEndpoints: {
            getById: MvcEndpoints.Helpdesk.Settings.EmailDistribution.GetById,
            create: MvcEndpoints.Helpdesk.Settings.EmailDistribution.Create,
            update: MvcEndpoints.Helpdesk.Settings.EmailDistribution.Update
        },
        subjectVariables: {
            'companyName': '$companyName$',
            'location': '$location$',
            'requestDate': '$requestDate$',
            'requestorName': '$requestorName$',
            'requestorSalutation': '$requestorSalutation$',
            'subject': '$subject$',
            'workRequestCode': '$workRequestCode$',
            'workTitle': '$workTitle$'
        }
    };

    let recipients = {
        to: [],
        cc: [],
        bcc: []
    };

    let subjectType = 'default';
    let fromType = 'default';

    document.addEventListener('DOMContentLoaded', function () {
        initializeForm();
        initializeSubjectTags();
        initializeRadioButtons();

        if (mode === 'edit' && distributionListId) {
            loadDistributionDetails();
        } else {
            document.getElementById('pageReferenceName').textContent = pageReference;
        }
    });

    function initializeForm() {
        document.getElementById('emailDistributionForm').addEventListener('submit', handleSubmit);

        document.getElementById('addToRecipientBtn').addEventListener('click', () => addRecipient('to'));
        document.getElementById('addCcRecipientBtn').addEventListener('click', () => addRecipient('cc'));
        document.getElementById('addBccRecipientBtn').addEventListener('click', () => addRecipient('bcc'));
    }

    function initializeRadioButtons() {
        const subjectRadios = document.querySelectorAll('input[name="subjectType"]');
        subjectRadios.forEach(radio => {
            radio.addEventListener('change', function () {
                subjectType = this.value;
                const customOptions = document.getElementById('customSubjectOptions');
                if (this.value === 'custom') {
                    customOptions.classList.add('show');
                } else {
                    customOptions.classList.remove('show');
                    document.getElementById('subjectPreview').classList.remove('show');
                }
            });
        });

        const fromRadios = document.querySelectorAll('input[name="fromType"]');
        fromRadios.forEach(radio => {
            radio.addEventListener('change', function () {
                fromType = this.value;
                const customFields = document.getElementById('customFromFields');
                if (this.value === 'custom') {
                    customFields.classList.add('show');
                } else {
                    customFields.classList.remove('show');
                }
            });
        });
    }

    function initializeSubjectTags() {
        const tags = document.querySelectorAll('.subject-tag');
        const textarea = document.getElementById('customSubjectText');

        tags.forEach(tag => {
            tag.addEventListener('click', function () {
                const value = this.getAttribute('data-value');
                const variable = CONFIG.subjectVariables[value];
                const cursorPos = textarea.selectionStart;
                const textBefore = textarea.value.substring(0, cursorPos);
                const textAfter = textarea.value.substring(cursorPos);

                textarea.value = textBefore + variable + textAfter;
                textarea.focus();

                const newCursorPos = cursorPos + variable.length;
                textarea.setSelectionRange(newCursorPos, newCursorPos);

                updateSubjectPreview();
            });
        });

        textarea.addEventListener('input', updateSubjectPreview);
    }

    function updateSubjectPreview() {
        const textarea = document.getElementById('customSubjectText');
        const preview = document.getElementById('subjectPreview');
        const previewContent = document.getElementById('subjectPreviewContent');

        if (textarea.value.trim() === '') {
            preview.classList.remove('show');
            return;
        }

        let previewText = textarea.value;

        previewText = previewText.replace(/\$companyName\$/g, 'ABC Company');
        previewText = previewText.replace(/\$location\$/g, 'Building A');
        previewText = previewText.replace(/\$requestDate\$/g, new Date().toLocaleDateString());
        previewText = previewText.replace(/\$requestorName\$/g, 'John Doe');
        previewText = previewText.replace(/\$requestorSalutation\$/g, 'Mr.');
        previewText = previewText.replace(/\$subject\$/g, 'Work Request');
        previewText = previewText.replace(/\$workRequestCode\$/g, 'WR-2024-001');
        previewText = previewText.replace(/\$workTitle\$/g, 'Maintenance Request');

        previewContent.textContent = previewText;
        preview.classList.add('show');
    }

    function addRecipient(type) {
        const nameInput = document.getElementById(`${type}RecipientName`);
        const emailInput = document.getElementById(`${type}RecipientEmail`);

        const name = nameInput.value.trim();
        const email = emailInput.value.trim();

        if (!name) {
            showNotification('Please enter recipient name', 'error');
            nameInput.focus();
            return;
        }

        if (!email) {
            showNotification('Please enter email address', 'error');
            emailInput.focus();
            return;
        }

        if (!validateEmail(email)) {
            showNotification('Please enter a valid email address', 'error');
            emailInput.focus();
            return;
        }

        const isDuplicate = recipients[type].some(r => r.email.toLowerCase() === email.toLowerCase());
        if (isDuplicate) {
            showNotification('This email address has already been added', 'warning');
            return;
        }

        recipients[type].push({
            name: name,
            email: email,
            type: type.toUpperCase()
        });

        nameInput.value = '';
        emailInput.value = '';
        nameInput.focus();

        renderRecipients(type);
        showNotification(`Recipient added to ${type.toUpperCase()}`, 'success');
    }

    function removeRecipient(type, index) {
        const recipient = recipients[type][index];
        recipients[type].splice(index, 1);
        renderRecipients(type);
        showNotification(`${recipient.name} removed from ${type.toUpperCase()}`, 'info');
    }

    function renderRecipients(type) {
        const container = document.getElementById(`${type}RecipientsContainer`);

        if (recipients[type].length === 0) {
            container.innerHTML = `
                <div class="empty-state">
                    <i class="ti ti-inbox"></i>
                    <p class="mb-0">No ${type === 'to' ? '' : type.toUpperCase() + ' '}recipients added yet</p>
                </div>
            `;
            return;
        }

        container.innerHTML = recipients[type].map((recipient, index) => `
            <div class="recipient-card">
                <div class="recipient-info">
                    <div class="recipient-name">${escapeHtml(recipient.name)}</div>
                    <div class="recipient-email">${escapeHtml(recipient.email)}</div>
                </div>
                <div class="recipient-actions">
                    <button type="button" class="btn btn-sm btn-danger" onclick="window.removeRecipient${type.charAt(0).toUpperCase() + type.slice(1)}(${index})" title="Remove">
                        <i class="ti ti-trash"></i>
                    </button>
                </div>
            </div>
        `).join('');
    }

    window.removeRecipientTo = function (index) { removeRecipient('to', index); };
    window.removeRecipientCc = function (index) { removeRecipient('cc', index); };
    window.removeRecipientBcc = function (index) { removeRecipient('bcc', index); };

    async function loadDistributionDetails() {
        try {
            const url = `${CONFIG.apiEndpoints.getById}?id=${distributionListId}`;
            const response = await fetch(url);
            const result = await response.json();

            if (result.success) {
                populateForm(result.data);
            } else {
                showNotification(result.message || 'Failed to load details', 'error');
            }
        } catch (error) {
            console.error('Error loading details:', error);
            showNotification('An error occurred while loading the data', 'error');
        }
    }

    function populateForm(data) {
        document.getElementById('pageReferenceName').textContent = data.pageReference || pageReference;

        if (data.subjectType === 'custom' && data.customSubject) {
            document.getElementById('subjectCustom').checked = true;
            document.getElementById('customSubjectOptions').classList.add('show');
            document.getElementById('customSubjectText').value = data.customSubject;
            updateSubjectPreview();
            subjectType = 'custom';
        }

        if (data.fromType === 'custom' && data.fromName && data.fromEmail) {
            document.getElementById('fromCustom').checked = true;
            document.getElementById('customFromFields').classList.add('show');
            document.getElementById('fromName').value = data.fromName;
            document.getElementById('fromEmail').value = data.fromEmail;
            fromType = 'custom';
        }

        if (data.recipients && Array.isArray(data.recipients)) {
            recipients.to = data.recipients.filter(r => r.type === 'TO');
            recipients.cc = data.recipients.filter(r => r.type === 'CC');
            recipients.bcc = data.recipients.filter(r => r.type === 'BCC');

            renderRecipients('to');
            renderRecipients('cc');
            renderRecipients('bcc');
        }
    }

    async function handleSubmit(e) {
        e.preventDefault();

        if (recipients.to.length === 0) {
            showNotification('At least one "To" recipient is required', 'error');
            return;
        }

        if (subjectType === 'custom') {
            const customSubjectText = document.getElementById('customSubjectText').value.trim();
            if (!customSubjectText) {
                showNotification('Custom subject cannot be empty', 'error');
                return;
            }
        }

        if (fromType === 'custom') {
            const fromName = document.getElementById('fromName').value.trim();
            const fromEmail = document.getElementById('fromEmail').value.trim();

            if (!fromName) {
                showNotification('From name is required when using custom from', 'error');
                return;
            }

            if (!fromEmail) {
                showNotification('From email is required when using custom from', 'error');
                return;
            }

            if (!validateEmail(fromEmail)) {
                showNotification('Please enter a valid from email address', 'error');
                return;
            }
        }

        const allRecipients = [
            ...recipients.to.map(r => ({ ...r, type: 'TO' })),
            ...recipients.cc.map(r => ({ ...r, type: 'CC' })),
            ...recipients.bcc.map(r => ({ ...r, type: 'BCC' }))
        ];

        const payload = {
            pageReference: pageReference,
            subjectType: subjectType,
            customSubject: subjectType === 'custom' ? document.getElementById('customSubjectText').value : null,
            fromType: fromType,
            fromName: fromType === 'custom' ? document.getElementById('fromName').value : null,
            fromEmail: fromType === 'custom' ? document.getElementById('fromEmail').value : null,
            recipients: allRecipients
        };

        const submitBtn = e.target.querySelector('button[type="submit"]');
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span>Saving...';

        try {
            let url, method;

            if (mode === 'edit') {
                url = `${CONFIG.apiEndpoints.update}?id=${distributionListId}`;
                method = 'PUT';
            } else {
                url = CONFIG.apiEndpoints.create;
                method = 'POST';
            }

            const response = await fetch(url, {
                method: method,
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(payload)
            });

            const result = await response.json();

            if (result.success) {
                showNotification(
                    mode === 'edit' ? 'Email distribution updated successfully' : 'Email distribution created successfully',
                    'success'
                );
                setTimeout(() => {
                    window.location.href = '/Helpdesk/EmailDistributionList';
                }, 1500);
            } else {
                showNotification(result.message || 'Failed to save email distribution', 'error');
            }
        } catch (error) {
            console.error('Error saving:', error);
            showNotification('An error occurred while saving', 'error');
        } finally {
            submitBtn.disabled = false;
            submitBtn.innerHTML = '<i class="ti ti-device-floppy me-1"></i>Save Email Distribution';
        }
    }

    function validateEmail(email) {
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
    }

    function escapeHtml(text) {
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text.replace(/[&<>"']/g, m => map[m]);
    }
})();
