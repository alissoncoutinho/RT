using System.Web;
using System.Web.Optimization;

namespace Barragem
{
    public class BundleConfig
    {
        // For more information on Bundling, visit http://go.microsoft.com/fwlink/?LinkId=254725

        public static void RegisterBundles(BundleCollection bundles)
        {
            BundleTable.EnableOptimizations = true;
            RegisterStyleBundles(bundles);
            RegisterJavascriptBundles(bundles);

        }

        private static void RegisterStyleBundles(BundleCollection bundles)
        {
            bundles.Add(new StyleBundle("~/css")
                            .Include("~/Content/bootstrap.css")
                            .Include("~/Content/bootstrap-theme.css")
                            .Include("~/Content/datepicker.css")
                            .Include("~/Content/bootstrap-dialog.css")
                            .Include("~/Content/chosen.min.css")
                            .Include("~/Content/bootstrap-chosen.css")
                            .Include("~/Content/flexslider/flexslider.css")
                            .Include("~/Content/jquery.Jcrop.min.css")
                            .Include("~/Content/font-awesome/css/font-awesome.css")
                            .Include("~/Content/AdminLTE.min.css")
                            .Include("~/Content/toastr.min.css"));


            bundles.Add(new StyleBundle("~/css0")
                            .Include("~/Content/styles.css"));

            bundles.Add(new StyleBundle("~/css1")
                            .Include("~/Content/styles.css"));

            bundles.Add(new StyleBundle("~/css2")
                            .Include("~/Content/styles-2.css"));

            bundles.Add(new StyleBundle("~/css3")
                            .Include("~/Content/styles-3.css"));

            bundles.Add(new StyleBundle("~/css4")
                            .Include("~/Content/styles-4.css"));
            bundles.Add(new StyleBundle("~/css5")
                            .Include("~/Content/styles-5.css"));
            bundles.Add(new StyleBundle("~/css6")
                            .Include("~/Content/styles-6.css"));
            bundles.Add(new StyleBundle("~/css7")
                            .Include("~/Content/styles-7.css"));

                            
        }

        private static void RegisterJavascriptBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/js")
                            .Include("~/Scripts/jquery-{version}.js")
                            .Include("~/Scripts/jquery-ui-{version}.js")
                            .Include("~/Scripts/bootstrap.js")
                            .Include("~/Scripts/bootstrap.typeahead.min.js")
                            .Include("~/Scripts/bootstrap-datepicker.js")
                            .Include("~/Scripts/jquery.mask.min.js")
                            .Include("~/Scripts/underscore-min.js")
                            .Include("~/Scripts/bootstrap-dialog.js")
                            .Include("~/Scripts/jquery.fileDownload.js")
                            .Include("~/Scripts/bootstrap-filestyle.min.js")
                            .Include("~/Scripts/jquery-migrate-1.2.1.min.js")
                            .Include("~/Scripts/bootstrap-hover-dropdown.min.js")
                            .Include("~/Scripts/back-to-top.js")
                            .Include("~/Scripts/FitVids/jquery.fitvids.js")
                            .Include("~/Scripts/flexslider/jquery.flexslider-min.js")
                            .Include("~/Scripts/barragem.js")
                            .Include("~/Scripts/jquery-placeholder/jquery.placeholder.js")
                            .Include("~/Scripts/jquery.confirm.js")
                            .Include("~/Scripts/main.js")
                            .Include("~/Scripts/adminlte.min.js")
                            .Include("~/Scripts/toastr.min.js"));


            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            bundles.Add(new ScriptBundle("~/bundles/chosen").Include(
                      "~/Scripts/chosen.jquery.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryform")
                            .Include("~/Scripts/jquery.form.js"));

            bundles.Add(new ScriptBundle("~/bundles/load-image")
                            .Include("~/Scripts/load-image.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/editor")
                            .Include("~/Scripts/tinymce/tinymce-min.js"));


        }


    }
}