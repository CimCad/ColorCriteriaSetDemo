using System;
using System.Text;
using System.Windows.Forms;

namespace ColorCriteriaSetDemo
{
    // Demonstrates the round-trip for reading a set's filter back out:
    //   1. FORWARD  - build a COLOR criteria set with a color filter.
    //   2. REVERSE  - read that filter back out of the existing set.
    //
    // ISet exposes no filter getter and ISetsFactory has no "get filter" method,
    // so the reverse step works by casting the set's COM object to IEntityQuery
    // (which it also implements) and calling GetFilter(), then casting the result
    // to the concrete FilterColor to read the color values.
    //
    // Interop type names are written out fully (no `using interop.*`) on purpose:
    // interop.CimBaseAPI and interop.CimMdlrAPI declare many of the same type
    // names (IEntityQuery, IEntityFilter, EFilterEnumType, ...), so unqualified
    // use trips CS0104 "ambiguous reference". Full qualification also makes the
    // snippet unambiguous to copy.
    internal class ColorCriteriaSetDemoCommand : CimUIInfrastructure.Commands.ICimWpfCommand
    {
        public bool OnCommand()
        {
            try
            {
                var appProvider = new interop.CimServicesAPI.CimApplicationProvider();
                var app = (interop.CimatronE.IApplication)appProvider.GetApplication();
                var doc = (interop.CimBaseAPI.ICimDocument)app.GetActiveDoc();
                if (doc == null)
                {
                    Warn("No active document. Open a part and run this command again.");
                    return true;
                }

                var container = (interop.CimMdlrAPI.IModelContainer)doc;
                var mdlModel = (interop.CimMdlrAPI.IModel)container.Model;

                const string setName = "DemoColorCriteriaSet";
                int targetColor = 0xFF0000;   // red, encoded 0xRRGGBB

                // ---- FORWARD: build a COLOR criteria set ----------------------
                // The model is the entity-query factory. Ask it for a color
                // filter, cast to the concrete FilterColor, and add the color(s).
                var query = (interop.CimMdlrAPI.IEntityQuery)mdlModel;
                var filter = query.CreateFilter(
                    interop.CimMdlrAPI.EFilterEnumType.cmFilterColor);
                var colorFilter = (interop.CimBaseAPI.FilterColor)filter;
                colorFilter.Add(targetColor);

                var factory = mdlModel.GetSetsFactory();   // interop.CimMdlrAPI.ISetsFactory
                // Idempotent: drop any prior run's set so CreateSet won't collide.
                try { factory.DeleteSet(setName); } catch { /* didn't exist yet */ }
                var set = factory.CreateSet(
                    setName, (interop.CimMdlrAPI.IEntityFilter)colorFilter);

                // ---- REVERSE: recover the filter from the existing set --------
                // ISet has no filter getter, but the same underlying COM object
                // also implements IEntityQuery. Cast to it and call GetFilter(),
                // then cast that filter to FilterColor to read the colors back.
                var setAsQuery = (interop.CimMdlrAPI.IEntityQuery)set;   // ISet -> IEntityQuery
                var recoveredFilter = setAsQuery.GetFilter();           // -> IEntityFilter
                var recoveredColor =
                    (interop.CimBaseAPI.FilterColor)recoveredFilter;    // -> FilterColor
                int[] recoveredColors = recoveredColor.GetFilter();     // recovered color ints

                // ---- Report --------------------------------------------------
                var sb = new StringBuilder();
                sb.AppendLine("Created color criteria set '" + setName + "'.");
                sb.AppendLine();

                // IsCriteria confirms the set is rule-based (not a static list).
                // Read it via the service-layer ISet view; ignore if unavailable.
                var setInfo = set as interop.CimServicesAPI.ISet;
                if (setInfo != null)
                {
                    try { sb.AppendLine("ISet.IsCriteria = " + setInfo.IsCriteria); }
                    catch { /* property not available on this build */ }
                }

                sb.AppendLine("Recovered " + recoveredColors.Length
                    + " color(s) from the set's filter:");
                foreach (int c in recoveredColors)
                    sb.AppendLine("    0x" + (c & 0xFFFFFF).ToString("X6"));

                MessageBox.Show(sb.ToString(), "Color Criteria Set Demo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Color Criteria Set Demo failed:\n\n" + ex.Message,
                    "Color Criteria Set Demo",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return true;
        }

        private static void Warn(string message)
        {
            MessageBox.Show(message, "Color Criteria Set Demo",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public bool OnCommandDblClk()
        {
            return OnCommand();
        }

        public CimUIInfrastructure.Commands.CimWpfUICommandStates OnCommandUI()
        {
            return new CimUIInfrastructure.Commands.CimWpfUICommandStates
            {
                UiState = CimUIInfrastructure.Commands.CommandUIState.Enabled
            };
        }

        public string GetAccelerator() => string.Empty;

        public void SetAccelerator(string accelerator) { }
    }
}
