/**
 * Company Contact Form Page (Add/Edit)
 * Handles phone and email management with modals
 */

(function ($) {
    'use strict';

    // Configuration
    const CONFIG = {
        endpoints: MvcEndpoints.CompanyContact
    };

    // Client context for multi-tab session safety
    const clientContext = {
        get idClient() { return window.PageContext?.idClient || 0; }
    };

    // State management
    const state = {
        phones: [],
        emails: [],
        editingPhoneIndex: null,
        editingEmailIndex: null,
        isEditMode: false
    };

    /**
     * Initialize the module
     */
    function init() {
        state.isEditMode = window.IsEditMode || false;

        // Initialize existing data for edit mode
        if (state.isEditMode) {
            state.phones = window.ExistingPhones || [];
            state.emails = window.ExistingEmails || [];
        }

        initializeSearchableDropdowns();
        bindEvents();
        renderPhoneList();
        renderEmailList();

        console.log('Company Contact Form page initialized');
    }

    /**
     * Initialize searchable dropdowns
     */
    function initializeSearchableDropdowns() {
        // Initialize all dropdowns with searchable attribute (excluding those in modals)
        $('select[data-searchable="true"]').not('.modal select').each(function () {
            // Check if already initialized
            if (!this._searchableDropdown) {
                new SearchableDropdown(this);
            }
        });
    }

    /**
     * Bind event handlers
     */
    function bindEvents() {
        // Form submission
        $('#contactForm').on('submit', handleFormSubmit);

        // Phone modal
        $('#addPhoneBtn').on('click', showAddPhoneModal);
        $('#savePhoneBtn').on('click', savePhone);

        // Email modal
        $('#addEmailBtn').on('click', showAddEmailModal);
        $('#saveEmailBtn').on('click', saveEmail);

        // Modal close events - reset forms
        $('#phoneModal').on('hidden.bs.modal', resetPhoneModal);
        $('#emailModal').on('hidden.bs.modal', resetEmailModal);

        // Initialize searchable dropdown in phone modal when it opens
        $('#phoneModal').on('shown.bs.modal', function () {
            const phoneTypeSelect = document.getElementById('phoneType');
            if (phoneTypeSelect && !phoneTypeSelect._searchableDropdown) {
                new SearchableDropdown(phoneTypeSelect);
            }
        });
    }

    /**
     * Show add phone modal
     */
    function showAddPhoneModal() {
        state.editingPhoneIndex = null;
        $('#phoneIndex').val('');
        $('#phoneId').val('0');
        $('#phoneType').val('').trigger('change');
        $('#phoneNumber').val('');
        $('#isMainPhone').prop('checked', false);

        const modal = new bootstrap.Modal(document.getElementById('phoneModal'));
        modal.show();
    }

    /**
     * Edit phone - make function global
     */
    window.editPhone = function (index) {
        const phone = state.phones[index];
        if (!phone) return;

        state.editingPhoneIndex = index;
        $('#phoneIndex').val(index);
        $('#phoneId').val(phone.IdPhone || phone.idPhone || 0);
        $('#phoneType').val(phone.IdPhoneType || phone.idPhoneType || '').trigger('change');
        $('#phoneNumber').val(phone.PhoneNumber || phone.phoneNumber || '');
        $('#isMainPhone').prop('checked', phone.IsMainPhone || phone.isMainPhone || false);

        const modal = new bootstrap.Modal(document.getElementById('phoneModal'));
        modal.show();
    };

    /**
     * Save phone
     */
    function savePhone() {
        // Validation
        const phoneType = $('#phoneType').val();
        const phoneNumber = $('#phoneNumber').val().trim();

        if (!phoneType) {
            $('#phoneType').addClass('is-invalid');
            $('#phoneTypeError').text('Phone type is required');
            return;
        }

        if (!phoneNumber) {
            $('#phoneNumber').addClass('is-invalid');
            $('#phoneNumberError').text('Phone number is required');
            return;
        }

        $('#phoneType').removeClass('is-invalid');
        $('#phoneNumber').removeClass('is-invalid');

        const isMainPhone = $('#isMainPhone').is(':checked');

        // Get phone type name
        const phoneTypeName = $('#phoneType option:selected').text();

        const phone = {
            IdPhone: parseInt($('#phoneId').val()) || 0,
            IdPhoneType: parseInt(phoneType),
            PhoneTypeName: phoneTypeName,
            PhoneNumber: phoneNumber,
            IsMainPhone: isMainPhone
        };

        // If setting as main, uncheck all others
        if (isMainPhone) {
            state.phones.forEach(p => p.IsMainPhone = false);
        }

        // Add or update
        if (state.editingPhoneIndex !== null) {
            state.phones[state.editingPhoneIndex] = phone;
        } else {
            state.phones.push(phone);
        }

        renderPhoneList();
        bootstrap.Modal.getInstance(document.getElementById('phoneModal')).hide();
        showNotification('Phone ' + (state.editingPhoneIndex !== null ? 'updated' : 'added') + ' successfully', 'success');
    }

    /**
     * Delete phone - make function global
     */
    window.deletePhone = function (index) {
        if (confirm('Are you sure you want to delete this phone number?')) {
            state.phones.splice(index, 1);
            renderPhoneList();
            showNotification('Phone deleted successfully', 'success');
        }
    };

    /**
     * Reset phone modal
     */
    function resetPhoneModal() {
        state.editingPhoneIndex = null;
        $('#phoneType').removeClass('is-invalid');
        $('#phoneNumber').removeClass('is-invalid');
        $('#phoneTypeError').text('');
        $('#phoneNumberError').text('');
    }

    /**
     * Show add email modal
     */
    function showAddEmailModal() {
        state.editingEmailIndex = null;
        $('#emailIndex').val('');
        $('#emailId').val('0');
        $('#emailAddress').val('');
        $('#isMainEmail').prop('checked', false);

        const modal = new bootstrap.Modal(document.getElementById('emailModal'));
        modal.show();
    }

    /**
     * Edit email - make function global
     */
    window.editEmail = function (index) {
        const email = state.emails[index];
        if (!email) return;

        state.editingEmailIndex = index;
        $('#emailIndex').val(index);
        $('#emailId').val(email.IdEmail || email.idEmail || 0);
        $('#emailAddress').val(email.EmailAddress || email.emailAddress || '');
        $('#isMainEmail').prop('checked', email.IsMainEmail || email.isMainEmail || false);

        const modal = new bootstrap.Modal(document.getElementById('emailModal'));
        modal.show();
    };

    /**
     * Save email
     */
    function saveEmail() {
        // Validation
        const emailAddress = $('#emailAddress').val().trim();

        if (!emailAddress) {
            $('#emailAddress').addClass('is-invalid');
            $('#emailAddressError').text('Email address is required');
            return;
        }

        // Basic email validation
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(emailAddress)) {
            $('#emailAddress').addClass('is-invalid');
            $('#emailAddressError').text('Please enter a valid email address');
            return;
        }

        $('#emailAddress').removeClass('is-invalid');

        const isMainEmail = $('#isMainEmail').is(':checked');

        const email = {
            IdEmail: parseInt($('#emailId').val()) || 0,
            EmailAddress: emailAddress,
            IsMainEmail: isMainEmail
        };

        // If setting as main, uncheck all others
        if (isMainEmail) {
            state.emails.forEach(e => e.IsMainEmail = false);
        }

        // Add or update
        if (state.editingEmailIndex !== null) {
            state.emails[state.editingEmailIndex] = email;
        } else {
            state.emails.push(email);
        }

        renderEmailList();
        bootstrap.Modal.getInstance(document.getElementById('emailModal')).hide();
        showNotification('Email ' + (state.editingEmailIndex !== null ? 'updated' : 'added') + ' successfully', 'success');
    }

    /**
     * Delete email - make function global
     */
    window.deleteEmail = function (index) {
        if (confirm('Are you sure you want to delete this email address?')) {
            state.emails.splice(index, 1);
            renderEmailList();
            showNotification('Email deleted successfully', 'success');
        }
    };

    /**
     * Reset email modal
     */
    function resetEmailModal() {
        state.editingEmailIndex = null;
        $('#emailAddress').removeClass('is-invalid');
        $('#emailAddressError').text('');
    }

    /**
     * Render phone list
     */
    function renderPhoneList() {
        const $list = $('#phoneList');

        if (state.phones.length === 0) {
            $list.html('<div class="empty-placeholder">-</div>');
            return;
        }

        let html = '';
        state.phones.forEach((phone, index) => {
            html += `
                <div class="phone-email-item">
                    <div class="item-info">
                        <span class="item-type">${escapeHtml(phone.PhoneTypeName || phone.phoneTypeName || 'Mobile')}</span>
                        <span>${escapeHtml(phone.PhoneNumber || phone.phoneNumber || '')}</span>
                        ${phone.IsMainPhone || phone.isMainPhone ? '<span class="main-badge"><i class="ti ti-checkbox me-1"></i>Main Phone</span>' : ''}
                    </div>
                    <div class="item-actions">
                        <button type="button" class="btn btn-sm btn-outline-primary" onclick="editPhone(${index})">
                            <i class="ti ti-pencil"></i>
                        </button>
                        <button type="button" class="btn btn-sm btn-outline-danger" onclick="deletePhone(${index})">
                            <i class="ti ti-trash"></i>
                        </button>
                    </div>
                </div>
            `;
        });

        $list.html(html);
    }

    /**
     * Render email list
     */
    function renderEmailList() {
        const $list = $('#emailList');

        if (state.emails.length === 0) {
            $list.html('<div class="empty-placeholder">-</div>');
            return;
        }

        let html = '';
        state.emails.forEach((email, index) => {
            html += `
                <div class="phone-email-item">
                    <div class="item-info">
                        <span>${escapeHtml(email.EmailAddress || email.emailAddress || '')}</span>
                        ${email.IsMainEmail || email.isMainEmail ? '<span class="main-badge"><i class="ti ti-checkbox me-1"></i>Main Email</span>' : ''}
                    </div>
                    <div class="item-actions">
                        <button type="button" class="btn btn-sm btn-outline-primary" onclick="editEmail(${index})">
                            <i class="ti ti-pencil"></i>
                        </button>
                        <button type="button" class="btn btn-sm btn-outline-danger" onclick="deleteEmail(${index})">
                            <i class="ti ti-trash"></i>
                        </button>
                    </div>
                </div>
            `;
        });

        $list.html(html);
    }

    /**
     * Handle form submission
     */
    function handleFormSubmit(e) {
        e.preventDefault();

        // Validation
        const contactName = $('#contactName').val().trim();
        const department = $('#department').val();

        if (!contactName) {
            $('#contactName').addClass('is-invalid');
            $('#contactNameError').text('Name is required');
            showNotification('Please fill in all required fields', 'error');
            return;
        }

        if (!department) {
            $('#department').addClass('is-invalid');
            $('#departmentError').text('Department is required');
            showNotification('Please fill in all required fields', 'error');
            return;
        }

        $('#contactName').removeClass('is-invalid');
        $('#department').removeClass('is-invalid');

        // Build payload
        const payload = {
            IdContact: state.isEditMode ? parseInt($('#contactId').val()) || 0 : 0,
            IdClient: clientContext.idClient,
            Title: $('#titlePrefix').val() || null,
            ContactName: contactName,
            IdDepartment: parseInt(department),
            RoleTitle: $('#roleTitle').val() || null,
            Notes: $('#notes').val() || null,
            Phones: state.phones,
            Emails: state.emails
        };

        // Get CSRF token
        const token = $('input[name="__RequestVerificationToken"]').val();

        // Disable submit button
        const $submitBtn = state.isEditMode ? $('#updateBtn') : $('#saveBtn');
        $submitBtn.prop('disabled', true)
            .html('<span class="spinner-border spinner-border-sm me-1"></span>Saving...');

        // Submit
        const endpoint = state.isEditMode ? CONFIG.endpoints.Update : CONFIG.endpoints.Create;
        const method = state.isEditMode ? 'PUT' : 'POST';

        $.ajax({
            url: endpoint,
            method: method,
            contentType: 'application/json',
            headers: {
                'RequestVerificationToken': token
            },
            data: JSON.stringify(payload),
            success: function (response) {
                if (response.success) {
                    showNotification('Contact ' + (state.isEditMode ? 'updated' : 'created') + ' successfully', 'success');
                    setTimeout(() => {
                        window.location.href = '/CompanyContact/Index';
                    }, 1000);
                } else {
                    showNotification(response.message || 'Failed to save contact', 'error');
                    $submitBtn.prop('disabled', false)
                        .html('<i class="ti ti-device-floppy me-1"></i>' + (state.isEditMode ? 'Update' : 'Save') + ' Contact');
                }
            },
            error: function (xhr, status, error) {
                console.error('Error saving contact:', error);

                let errorMessage = 'Error saving contact. Please try again.';

                if (xhr.responseText) {
                    try {
                        const response = JSON.parse(xhr.responseText);
                        if (response.errors && Array.isArray(response.errors) && response.errors.length > 0) {
                            errorMessage = response.errors.join(', ');
                        } else if (response.message) {
                            errorMessage = response.message;
                        }
                    } catch (e) {
                        console.error('Error parsing response:', xhr.responseText);
                    }
                }

                showNotification(errorMessage, 'error');
                $submitBtn.prop('disabled', false)
                    .html('<i class="ti ti-device-floppy me-1"></i>' + (state.isEditMode ? 'Update' : 'Save') + ' Contact');
            }
        });
    }

    /**
     * Utility: Escape HTML
     */
    function escapeHtml(text) {
        if (!text) return '';
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return String(text).replace(/[&<>"']/g, m => map[m]);
    }

    // Initialize when document is ready
    $(document).ready(function () {
        init();
    });

})(jQuery);
