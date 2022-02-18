using System;
using System.Collections.Generic;
using System.Linq;
using MCTools.Enums;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MCTools.Shared.Dialog
{
    public partial class TexturesBlacklistDialog : LayoutComponentBase
    {
        [CascadingParameter] MudDialogInstance MudDialog { get; set; }

        [Parameter] public MCEdition Edition { get; set; }
        [Parameter] public List<string> Blacklist { get; set; }
        [Parameter] public List<string> DefaultBlacklist { get; set; }
        [Parameter] public Action<MCEdition, List<string>> Callback { get; set; }

        private string BlacklistEdited { get; set; }

        protected override void OnInitialized()
        {
            BlacklistEdited = string.Join("\n", Blacklist);
        }

        private void Submit()
        {
            Callback(Edition, BlacklistEdited.Split('\n').ToList());
            MudDialog.Close(DialogResult.Ok(true));
        }

        private void Reset()
        {
            BlacklistEdited = string.Join("\n", DefaultBlacklist);
        }

        private void Cancel() => MudDialog.Cancel();
    }
}
