using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Data.Templates;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Shell.Framework;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Sitecore.Support.Shell.Framework.Pipelines
{
    public class AddFromTemplate
    {
        public void Execute(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (args.HasResult)
            {
                int index = args.Result.IndexOf(',');
                Assert.IsTrue(index >= 0, "Invalid return value from dialog");
                string path = StringUtil.Left(args.Result, index);
                string name = StringUtil.Mid(args.Result, index + 1);
                Database database = Factory.GetDatabase(args.Parameters["database"]);
                string str3 = args.Parameters["id"];
                string str4 = args.Parameters["language"];
                Item parent = database.Items[str3, Language.Parse(str4)];
                if (parent == null)
                {
                    SheerResponse.Alert("Parent item not found.", new string[0]);
                    args.AbortPipeline();
                }
                else if (!parent.Access.CanCreate())
                {
                    SheerResponse.Alert("You do not have permission to create items here.", new string[0]);
                    args.AbortPipeline();
                }
                else
                {
                    Item item = database.GetItem(path, Language.Parse(str4));
                    if (item == null)
                    {
                        SheerResponse.Alert("Item not found.", new string[0]);
                        args.AbortPipeline();
                    }
                    else
                    {
                        Item item3;
                        History.Template = item.ID.ToString();
                        if (item.TemplateID == TemplateIDs.Template)
                        {
                            string[] parameters = new string[] { AuditFormatter.FormatItem(item) };
                            Log.Audit(this, "Add from template: {0}", parameters);
                            TemplateItem template = item;
                            item3 = Context.Workflow.AddItem(name, template, parent);
                        }
                        else
                        {
                            string[] textArray2 = new string[] { AuditFormatter.FormatItem(item) };
                            Log.Audit(this, "Add from branch: {0}", textArray2);
                            BranchItem branch = item;
                            item3 = Context.Workflow.AddItem(name, branch, parent);
                            if ((item3.Fields["__Source"] != null) && (item3.Fields["__Source Item"] != null))
                            {
                                item3.Editing.BeginEdit();
                                foreach (Field field in item3.Fields)
                                {
                                    if (!IsStandardTempalteField(field))
                                    {
                                        field.Reset();
                                    }
                                }
                                item3.Editing.EndEdit();
                            }
                        }
                        args.CarryResultToNextProcessor = true;
                        if (item3 == null)
                        {
                            args.AbortPipeline();
                        }
                        else
                        {
                            args.Result = item3.ID.ToString();
                        }
                    }
                }
            }
        }

        public void GetTemplate(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (SheerResponse.CheckModified())
            {
                UrlString str = new UrlString(UIUtil.GetUri("control:AddFromTemplate"));
                string template = History.Template;
                if ((template != null) && (template.Length > 0))
                {
                    str.Append("fo", template);
                }
                Context.ClientPage.ClientResponse.ShowModalDialog(str.ToString(), "1200px", "700px", "", true);
                args.WaitForPostBack(false);
            }
        }

        public static bool IsStandardTempalteField(Field field)
        {
            Template template = TemplateManager.GetTemplate(Settings.DefaultBaseTemplate, field.Database);
            Assert.IsNotNull(template, "template");
            return template.ContainsField(field.ID);
        }
    }
}
