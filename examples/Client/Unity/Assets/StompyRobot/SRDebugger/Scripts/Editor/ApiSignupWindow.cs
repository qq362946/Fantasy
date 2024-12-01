using UnityEditor;
using UnityEngine;

namespace SRDebugger.Editor
{
    class ApiSignupTermsWindow : EditorWindow
    {
        public static void Open()
        {
            GetWindowWithRect<ApiSignupTermsWindow>(new Rect(0, 0, 430, 345), true, "SRDebugger - Bug Reporter TOS",
                true);
        }

        private void OnGUI()
        {
            GUILayout.Label("Terms and Conditions", SRInternalEditorUtil.Styles.HeaderLabel);

            GUILayout.Label(
                "The Bug Reporter service is provided free of charge to owners of SRDebugger. One valid license key of SRDebugger allows one account to be registered. You must not share your API key with another party. Stompy Robot LTD reserves the right to terminate your bug reporter account if your API key is shared with another party.",
                SRInternalEditorUtil.Styles.ParagraphLabel);

            GUILayout.Label(
                "Stompy Robot LTD reserves the right to cancel the bug report service at any time without notice.",
                SRInternalEditorUtil.Styles.ParagraphLabel);

            GUILayout.Label(
                "By signing up for the Bug Reporter service you grant Stompy Robot LTD permission to gather non-identifying information from users when submitting reports. You attest that your users are aware of the data collection and give their consent.",
                SRInternalEditorUtil.Styles.ParagraphLabel);

            GUILayout.Label(
                "THE SERVICE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.",
                SRInternalEditorUtil.Styles.ParagraphLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Close"))
            {
                Close();
            }
        }
    }
}
