using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Windows.Forms;

namespace DockerHostSwitcher.WinForms
{
    public class NotifyIconMenu : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ImageList _imageList;
        private readonly Image _dockerLogo;
        private readonly Icon _dockerIcon;
        private readonly string _settingsFilePath;

        public NotifyIconMenu()
        {
            var codeBasePath = GetEntryAssemblyRootPath();
            _settingsFilePath = Path.Combine(codeBasePath ?? Directory.GetCurrentDirectory(), "docker-hosts.json");

            _dockerLogo = ResourceUtils.ReadDockerLogo();
            _dockerIcon = ResourceUtils.ReadDockerIcon();

            _notifyIcon = new NotifyIcon
            {
                Icon = _dockerIcon,
                Text = "Docker Host Switcher",
                Visible = true,
            };
            _notifyIcon.MouseUp += NotifyIcon_MouseUp;

            _imageList = new ImageList();
            _imageList.Images.Add(_dockerLogo);
        }

        private static string? GetCurrentDockerHost() =>
            Environment.GetEnvironmentVariable("DOCKER_HOST", EnvironmentVariableTarget.User);

        public void SetCurrentDockerHost(string? value) => Environment.SetEnvironmentVariable("DOCKER_HOST",
            string.IsNullOrWhiteSpace(value) ? null : value, EnvironmentVariableTarget.User);

        private static string? GetEntryAssemblyRootPath()
        {
            var codeBase = Assembly.GetEntryAssembly()?.CodeBase ??
                           throw new InvalidOperationException("No entry assembly or codebase found");
            return Path.GetDirectoryName(new Uri(codeBase).LocalPath);
        }

        public void BrowseJsonDirectory()
        {
            Process.Start("explorer.exe", "/select,\"" + _settingsFilePath + "\"");
        }

        private void NotifyIcon_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            if (!(sender is NotifyIcon notifyIcon)) return;
            var currentStrip = notifyIcon.ContextMenuStrip;
            notifyIcon.ContextMenuStrip = null;
            DisposeToolStrip(currentStrip);
            var contextMenuStrip = BuildContextMenuStrip();
            if (contextMenuStrip is null)
                return;
            notifyIcon.ContextMenuStrip = contextMenuStrip;
            ShowContextMenu();
        }

        private string[] ReadAvailableHosts()
        {
            var hostsJson = File.ReadAllText(_settingsFilePath);
            return JsonSerializer.Deserialize<AvailableDockerHostsConfiguration>(hostsJson)?.Hosts ??
                   throw new InvalidOperationException("Invalid Json configuration");
        }

        private ContextMenuStrip? BuildContextMenuStrip()
        {
            var contextMenuStrip = new ContextMenuStrip {ImageList = _imageList};
            contextMenuStrip.Closed += (o, e) =>
            {
                if (ReferenceEquals(_notifyIcon.ContextMenuStrip, o))
                    _notifyIcon.ContextMenuStrip = null;
            };
            contextMenuStrip.ItemClicked += (o, e) =>
            {
                if (e.ClickedItem.Tag is Action act)
                    act.Invoke();
            };
            var items = BuildMenuItems();
            if (items is null || !items.Any())
            {
                contextMenuStrip.Dispose();
                return null;
            }

            contextMenuStrip.Items.AddRange(items);
            contextMenuStrip.PerformLayout();
            return contextMenuStrip;
        }

        protected ToolStripItem[]? BuildMenuItems()
        {
            string[] hosts;
            try
            {
                hosts = ReadAvailableHosts();
            }
            catch
            {
                if (MessageBox.Show("Invalid Json Configuration. Do you want to close the application?", "Error",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Error) == DialogResult.Yes)
                {
                    Program.ExitApplication();
                }

                return null;
            }

            var result = new List<ToolStripItem>(hosts.Length + 6);
            if (!hosts.Any())
            {
                var label = new ToolStripLabel("No hosts defined in JSON file");
                label.Font = new Font(label.Font, FontStyle.Bold);
                result.Add(label);
            }
            else
            {
                var label = new ToolStripLabel("Docker hosts");
                label.Font = new Font(label.Font, FontStyle.Bold);
                label.ImageIndex = 0;
                result.Add(label);
                result.Add(new ToolStripSeparator());
            }

            var currentHost = GetCurrentDockerHost();
            result.AddRange(hosts.Select(host => BuildHostMenuItem(host, currentHost)));

            result.Add(new ToolStripSeparator());
            result.Add(new ToolStripMenuItem("Browse for docker_hosts.json") {Tag = (Action) BrowseJsonDirectory});
            result.Add(new ToolStripSeparator());
            result.Add(new ToolStripMenuItem("Close") {Tag = (Action) Program.ExitApplication});
            return result.ToArray();
        }

        private ToolStripMenuItem BuildHostMenuItem(string host, string? currentHost)
        {
            FontStyle fontStyle = default;
            string? actualHost = host;
            var label = host;
            var isCurrent = string.Equals(actualHost, currentHost, StringComparison.InvariantCulture);

            if (string.IsNullOrWhiteSpace(host) || string.Equals(host, "none", StringComparison.InvariantCulture))
            {
                actualHost = null;
                label = "System default";
                fontStyle = FontStyle.Italic;
                isCurrent = string.IsNullOrWhiteSpace(currentHost);
            }

            void Action() => SetCurrentDockerHost(actualHost);
            var btn = new ToolStripMenuItem(label) {Tag = (Action) Action, Checked = isCurrent};
            if (fontStyle != default) btn.Font = new Font(btn.Font, fontStyle);
            return btn;
        }

        private static void DisposeToolStrip(ToolStrip menuStrip)
        {
            if (menuStrip is null) return;
            menuStrip.Items.Clear();
            menuStrip.Dispose();
        }

        private static MethodInfo? _showContextMenuMethodInfo = null;

        private void ShowContextMenu()
        {
            if(_showContextMenuMethodInfo == null)
                _showContextMenuMethodInfo = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new InvalidOperationException("No notify icon");
            _showContextMenuMethodInfo?.Invoke(this._notifyIcon, null);
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.ContextMenuStrip?.Dispose();
            _notifyIcon.Dispose();
            _dockerIcon?.Dispose();
            _dockerLogo?.Dispose();
            _imageList?.Dispose();
        }
    }
}