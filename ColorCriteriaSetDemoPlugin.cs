using System;
using System.IO;
using System.Reflection;

namespace ColorCriteriaSetDemo
{
    // Cimatron discovers this class by interface at load time and calls
    // AppendCommand() to register the plugin's toolbar entry.
    //
    // NOTE: the ExternalCommands.ini key must point at THIS class (the
    // ICimApiCommandPlugin), not at ColorCriteriaSetDemoCommand.
    public class ColorCriteriaSetDemoPlugin : CimUIInfrastructure.PlugIn.ICimApiCommandPlugin
    {
        public CimUIInfrastructure.PlugIn.ApiCommand AppendCommand()
        {
            string pluginDir = GetExecutionPath();
            string icoPath = Path.Combine(pluginDir, "ColorCriteriaSetDemo.ico");
            EnsureToolbarIconCache(icoPath);

            var command = new CimUIInfrastructure.PlugIn.ApiCommand
            {
                Name = "ColorCriteriaSetDemo",
                ToolbarName = "APIs",
                MenuPath = "API\nColorCriteriaSetDemo",
                Caption = "Color Criteria Set Demo",
                ToolTip = "Create a color criteria set, then read its filter back out",
                Description = "Creates a color criteria set and recovers the color filter from the "
                            + "set by casting ISet -> IEntityQuery -> GetFilter() -> FilterColor.",
                // Part: a color criteria set is a rule, so this runs against any open
                // part document without needing pre-colored geometry.
                Application =
                    CimUIInfrastructure.PlugIn.ApiApplications.Part,
                IconSource = new CimWpfContracts.WpfImageIdentifier(
                    icoPath,
                    CimWpfContracts.ImageSize.Small),
                ExecuteCommand = new ColorCriteriaSetDemoCommand()
            };

            return command;
        }

        public static string GetExecutionPath()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var location = assembly?.Location;
            return string.IsNullOrEmpty(location)
                ? AppDomain.CurrentDomain.BaseDirectory
                : Path.GetDirectoryName(location);
        }

        // Cimatron's WpfImageIdentifier renders the toolbar icon from a sibling
        // 32x32 .png cached next to the source .ico. When the cache is absent on
        // first launch after a fresh install, Cimatron's regeneration path (under
        // @1 INI re-read) has been observed to leave the toolbar button blank.
        // Materializing the cache here makes first-launch behavior deterministic.
        // Read-only Program Files is handled by silently falling through; in that
        // case Cimatron's own cache regen still runs as before.
        private static void EnsureToolbarIconCache(string icoPath)
        {
            string pngPath = Path.ChangeExtension(icoPath, ".png");
            if (File.Exists(pngPath) || !File.Exists(icoPath)) return;
            try
            {
                using (var icon = new System.Drawing.Icon(icoPath, 32, 32))
                using (var bmp = icon.ToBitmap())
                {
                    bmp.Save(pngPath, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
            catch
            {
                // Plugin runs at the user's integrity level; the seed .ico may
                // be malformed (PNG-in-ICO trips Icon.ToBitmap) or Program Files
                // may be read-only. Either way, fall through and let Cimatron
                // attempt its own cache regen.
            }
        }
    }
}
