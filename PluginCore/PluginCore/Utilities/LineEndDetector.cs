namespace PluginCore.Utilities
{
    public class LineEndDetector
    {
        /// <summary>
        /// Gets the correct EOL marker
        /// </summary>
        public static string GetNewLineMarker(int eolMode)
        {
            return eolMode switch
            {
                1 => "\r",
                2 => "\n",
                _ => "\r\n",
            };
        }

        /// <summary>
        /// Basic detection of text's EOL marker
        /// </summary>
        public static int DetectNewLineMarker(string text, int defaultMarker)
        {
            int cr = text.IndexOf('\r');
            int lf = text.IndexOf('\n');
            if ((cr >= 0) && (lf >= 0))
            {
                if (cr < lf) return 0;
                return 2;
            }

            if ((cr < 0) && (lf < 0))
            {
                return (int)PluginBase.MainForm.Settings.EOLMode;
            }
            if (lf < 0) return 1;
            return 2;
        }

    }

}
