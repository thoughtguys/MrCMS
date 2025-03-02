export function initiateContentTemplate() {
    // Cache jQuery selectors
    const $contentTemplateContainer = $('[data-content-template-container]');
    const $contentTemplateForm = $contentTemplateContainer.closest('form');

    // Early return if form doesn't exist
    if (!$contentTemplateForm.length) return;

    function cleanup() {
        $(document).off('click.repeatableTokens');
        $contentTemplateForm.off('submit.contentTemplate');
        $('[content-template-drag] [data-toggle="popover"]').popover('dispose');

        if ($contentTemplateForm.data('validator')) {
            $contentTemplateForm.data('validator').destroy();
        }

        if (typeof CKEDITOR !== 'undefined') {
            Object.keys(CKEDITOR.instances).forEach(instance => {
                CKEDITOR.instances[instance].destroy();
            });
        }

        document.querySelectorAll('[content-template-drag]').forEach(el => {
            el.removeEventListener('dragstart', handleDragStart);
        });
    }

    function initializeRepeatableTokens() {
        $(document).on('click.repeatableTokens', '.add-repeatable-item', function () {
            const $btn = $(this);
            const $repeatableToken = $btn.closest('.repeatable-token');
            const $inputArea = $repeatableToken.find('.repeatable-input-area');
            const $container = $repeatableToken.find('.repeatable-items-container');

            // Validate input area fields
            if (!$inputArea.find('input, select, textarea').valid()) {
                return;
            }

            // Create new item based on input area values
            const $newItemContent = $inputArea.clone();
            const itemIndex = $container.find('.repeatable-item').length;
            const repeatableName = $repeatableToken.data('repeatableName');
            
            // Update IDs and names for the new item
            $newItemContent.find('[id]').each(function () {
                const $el = $(this);
                const fieldName = $el.data('field-name');
                if (fieldName) {
                    // Update ID
                    $el.attr('id', `${repeatableName}_${itemIndex}_${fieldName}`);
                    // Update name with repeatable index
                    $el.attr('name', `${repeatableName}[${itemIndex}].${fieldName}`);
                }
            });

            // Create repeatable item container
            const $repeatableItem = $('<div class="repeatable-item" data-index="' + itemIndex + '"></div>')
                .append('<div class="repeatable-item-content"></div>')
                .append('<button type="button" class="btn btn-danger btn-sm remove-repeatable-item"><i class="fa fa-trash"></i></button><hr class="my-3">');

            // Add content to container
            $repeatableItem.find('.repeatable-item-content').append($newItemContent.contents());
            $container.append($repeatableItem);

            // Add validation rules to new elements
            $repeatableItem.find('input, select, textarea').each(function () {
                $(this).rules('add', {
                    required: true,
                    messages: {
                        required: 'This field is required.'
                    }
                });
            });

            // Clear input area
            $inputArea.find('input, select, textarea').val('');
            $inputArea.find('input:checkbox').prop('checked', false);
            $inputArea.find('select').prop('selectedIndex', 0);
        });

        // Handle repeatable item removal
        $(document).on('click.repeatableTokens', '.remove-repeatable-item', function () {
            const $item = $(this).closest('.repeatable-item');
            const $container = $item.closest('.repeatable-items-container');

            $item.remove();

            // Reindex remaining items
            $container.find('.repeatable-item').each(function(index) {
                const $repeatableItem = $(this);
                $repeatableItem.attr('data-index', index);

                // Update field names and IDs
                $repeatableItem.find('[name]').each(function() {
                    const $field = $(this);
                    const fieldName = $field.data('field-name');
                    const repeatableName = $field.closest('.repeatable-token').data('repeatableName');

                    if (fieldName) {
                        $field.attr('name', `${repeatableName}[${index}].${fieldName}`);
                        $field.attr('id', `${repeatableName}_${index}_${fieldName}`);
                    }
                });
            });
        });
    }

    function serializeContentTemplateForm() {
        const formData = {};
        const repeatableData = {};

        // Collect form data
        $contentTemplateForm.find('[name]:not([name="__RequestVerificationToken"])').each(function () {
            const el = this;
            const name = el.getAttribute('name');
            let value;

            // Handle CKEditor instances
            if (el.tagName.toLowerCase() === 'textarea' &&
                typeof CKEDITOR !== 'undefined' &&
                CKEDITOR.instances[el.id]) {
                value = CKEDITOR.instances[el.id].getData();
            } else {
                value = el.type === 'checkbox' ? el.checked : el.value;
            }

            // Process repeatable fields
            if (name.includes('[')) {
                const matches = name.match(/^(.+?)\[(\d+)\]\.(.+)$/);
                if (matches) {
                    const [, repeatableName, index, fieldName] = matches;
                    repeatableData[repeatableName] = repeatableData[repeatableName] || {};
                    repeatableData[repeatableName][index] = repeatableData[repeatableName][index] || {};
                    repeatableData[repeatableName][index][fieldName] = value;
                }
            } else {
                formData[name] = value;
            }
        });

        // Convert repeatable data to final format
        for (const repeatableName in repeatableData) {
            const repeatableItems = [];
            const indices = Object.keys(repeatableData[repeatableName]).sort((a, b) => parseInt(a) - parseInt(b));

            for (const index of indices) {
                const itemData = {};
                // Convert object to dictionary format
                Object.entries(repeatableData[repeatableName][index]).forEach(([key, value]) => {
                    itemData[key] = value;
                });
                repeatableItems.push(itemData);
            }

            formData[repeatableName] = JSON.stringify(repeatableItems);
        }

        return JSON.stringify(formData);
    }

    function initializeContentTemplateValidation() {
        $contentTemplateForm.validate({
            errorClass: 'text-danger',
            errorElement: 'span',
            errorPlacement: function (error, element) {
                error.addClass('field-validation-error');
                error.insertAfter(element);
            },
            highlight: function (element) {
                $(element).addClass('is-invalid').removeClass('is-valid');
            },
            unhighlight: function (element) {
                $(element).addClass('is-valid').removeClass('is-invalid');
            }
        });
    }

    function initFormSubmit() {
        $contentTemplateForm.on('submit.contentTemplate', function (e) {
            e.preventDefault();
            if (!$(this).valid()) return;

            const jsonData = serializeContentTemplateForm();

            // Remove existing hidden input if it exists
            $contentTemplateForm.find('input[name="Properties"]').remove();

            // Add serialized data as hidden input
            $('<input>').attr({
                type: 'hidden',
                name: 'Properties',
                value: jsonData
            }).appendTo($(this));
        });
    }
    
    // Initialize everything
    cleanup();
    initializeContentTemplateValidation();
    initializeRepeatableTokens();
    initFormSubmit();
}

export function initContentTemplateTokens(){
    function initializeDragAndDrop() {
        const dragElements = document.querySelectorAll('[content-template-drag]');
        dragElements.forEach(el => {
            el.addEventListener('dragstart', handleDragStart);
        });
    }

    function handleDragStart(e) {
        e.dataTransfer.effectAllowed = 'move';
        e.dataTransfer.setData('Text', e.target.dataset.text);
    }

    function initPopovers() {
        const $popovers = $('[content-template-drag] [data-toggle="popover"]');
        $popovers.popover({
            trigger: 'click'
        });
    }

    initializeDragAndDrop();
    initPopovers();
}