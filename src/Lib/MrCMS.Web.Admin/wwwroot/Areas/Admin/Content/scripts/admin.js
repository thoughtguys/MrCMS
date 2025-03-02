import {ajaxSetup, showHideLoading} from './ajax-config'
import {setupConfirmation} from './setup-confirmation'
import {initializeBootstrapPlugins} from "./initialize-bootstrap-plugins";
import {setupGlobalization} from "./setup-globalisation";
import {setupDatePickers} from "./setup-jquery-ui";
import {setupAsyncPaging} from "./async-paging";
import {setupFeatherlight} from "./setup-featherlight";
import {initializePlugins, registerGlobalFunctions} from "./initialize-plugins";
import {registerPostToUrl} from "./post-to-url";
import {postToUrlHelper} from "./post-to-url";
import {setupResizeModal} from "./resize-modal";
import {registerDelayKeyup} from "./delay-keyup";
import {registerPlugins} from './media-selector';
import {setupHealthChecks} from "./health-checks";
import {registerUpdateArea} from "./update-area";
import {setupAddWebpage} from "./add-webpage";
import {setupWebpageTree} from "./pages";
import {setupEditWebpage} from "./edit-webpage";
import {setupMediaCategoryPage} from "./media-category";
import {initMediaUploader, MediaUploader} from './media-uploader';
import {setupLayoutTree} from "./layout";
import {setupMenu} from "./menu";
import {setupStickyTabs} from "./sticky-tabs";
import {setupUserAvatar} from "./user-avatar";
import {setupTransientNotifications} from "./transient-notifications";
import {setupNotificationBar} from "./persistent-notifications";
import {setupRegistrationsAdminReport} from "./user-registrations-report";
import {setupMediaCategory} from "./add-category";
import {setupSiteSearch} from "./search";
import {setupFormBuilder} from "./form-builder"
import {setupMessageTemplateEditor} from "./message-template"
import {setupMessageTemplatePreview} from "./message-template"
import {setupAddPageTemplate} from "./add-page-template";
import {setupSortForm} from "./form-sort";
import {setupAddPageWidget} from "./add-page-widget";
import {setupSimpleFiles} from "./media-show-files-simple";
import {setupSiteCopyOptions} from "./add-site"
import {setupBatchFunctions} from "./batch.js"
import {setupResourceChooseSite} from "./choose-site";
import {setupWebpageSelect2} from "./webpage-search";
import {setupContentAdmin} from "./content-admin";
import {initIFrameAutoHeight} from "./iframe-auto-height";
import {SetupCronParser} from './cron-parser.js';
import {initiateContentTemplate, initContentTemplateTokens} from "./content-template";
import {initializeHtmlEditor} from "./html-editor";
import {initializeCssEditor} from "./css-editor";
import {initializeJavaScriptEditor} from "./js-editor";
import {setupWebpageUrlSelector} from "./webpage-url-selector";
import {initializeTextEditor} from "./text-editor";

ajaxSetup();
$(() => {
    initIFrameAutoHeight();
    registerPlugins();
    setupConfirmation();
    showHideLoading();
    initializeBootstrapPlugins()
    setupGlobalization()
    setupAsyncPaging();
    setupFeatherlight()
    registerPostToUrl();
    setupResizeModal()
    registerDelayKeyup();
    setupHealthChecks();
    registerUpdateArea();
    setupAddWebpage();
    setupEditWebpage();
    setupWebpageTree();
    setupLayoutTree();
    setupMediaCategory();
    initMediaUploader();
    setupDatePickers();
    setupMediaCategoryPage();
    registerGlobalFunctions();
    setupTransientNotifications();
    setupMenu();
    setupStickyTabs();
    setupUserAvatar();
    setupRegistrationsAdminReport();
    setupNotificationBar();
    postToUrlHelper();
    setupSiteSearch();
    setupFormBuilder();
    setupMessageTemplateEditor();
    setupMessageTemplatePreview();
    setupAddPageTemplate();
    setupSortForm();
    setupAddPageWidget();
    setupSiteCopyOptions();
    setupBatchFunctions();
    setupResourceChooseSite();
    setupWebpageSelect2();
    SetupCronParser();
    initiateContentTemplate();
    initContentTemplateTokens();
    initializeHtmlEditor();
    initializeCssEditor();
    initializeJavaScriptEditor();
    setupWebpageUrlSelector();
    $(document).on("initialize-plugins", initiateContentTemplate);
    $(document).on("initialize-plugins", initializeHtmlEditor);
    $(document).on("initialize-plugins", initializeCssEditor);
    $(document).on("initialize-plugins", initializeJavaScriptEditor);
    $(document).on("initialize-plugins", setupWebpageUrlSelector);
    $(document).on("initialize-plugins", () => initializeTextEditor());

    let mediaUploader = new MediaUploader($(document), {});
    mediaUploader.init();

    //Default theme for select2 (Bootstrap 4)
    $.fn.select2.defaults.set("theme", "bootstrap4");

    if (Dropzone) {
        Dropzone.autoDiscover = false;
    }

    $(document).on('click', 'a.more-link', function () {
        return false;
    });

    $(document).on('change', '#admin-site-selector', function () {
        location.href = $(this).val();
    });


    initializePlugins();

    setupSimpleFiles();

    setupContentAdmin();
})
