using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using Palisades.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Palisades.Services
{
    /// <summary>
    /// Service IMAP pour compter les mails non lus et récupérer les sujets (Zimbra, etc.).
    /// </summary>
    public class ImapMailService
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;
        private ImapClient? _client;
        private readonly object _clientLock = new object();

        public ImapMailService(string host, int port, string username, string password)
        {
            _host = host ?? "";
            _port = port > 0 ? port : 993;
            _username = username ?? "";
            _password = password ?? "";
        }

        public bool IsConnected
        {
            get { lock (_clientLock) { return _client?.IsConnected == true; } }
        }

        public async Task ConnectAsync()
        {
            if (string.IsNullOrWhiteSpace(_host))
                throw new InvalidOperationException("IMAP host is required.");
            lock (_clientLock)
            {
                if (_client?.IsConnected == true)
                    return;
                _client?.Dispose();
                _client = new ImapClient();
            }
            await _client.ConnectAsync(_host, _port, SecureSocketOptions.SslOnConnect).ConfigureAwait(false);
            await _client.AuthenticateAsync(_username, _password).ConfigureAwait(false);
        }

        public async Task DisconnectAsync()
        {
            lock (_clientLock)
            {
                if (_client == null) return;
                if (_client.IsConnected)
                    _client.Disconnect(false);
                _client.Dispose();
                _client = null;
            }
            await Task.CompletedTask.ConfigureAwait(false);
        }

        /// <summary>Retourne le nombre de messages non lus dans le dossier (IMAP STATUS).</summary>
        public async Task<int> GetUnreadCountAsync(string folderName)
        {
            EnsureConnected();
            IMailFolder folder;
            if (string.Equals(folderName, "INBOX", StringComparison.OrdinalIgnoreCase))
                folder = _client!.Inbox;
            else
                folder = await _client!.Inbox.GetSubfolderAsync(folderName).ConfigureAwait(false);
            var status = await folder.StatusAsync(StatusItems.Unread).ConfigureAwait(false);
            return status.Unread ?? 0;
        }

        /// <summary>Liste des noms de dossiers (INBOX + sous-dossiers de l'Inbox).</summary>
        public async Task<List<string>> GetFolderNamesAsync()
        {
            EnsureConnected();
            var list = new List<string> { "INBOX" };
            var folders = await _client!.Inbox.GetSubfoldersAsync().ConfigureAwait(false);
            foreach (var f in folders)
                list.Add(f.Name);
            return list;
        }

        /// <summary>Récupère les N derniers mails non lus (enveloppe : expéditeur, sujet, date).</summary>
        public async Task<List<MailSummaryItem>> GetRecentUnreadSubjectsAsync(string folderName, int maxCount)
        {
            EnsureConnected();
            IMailFolder folder;
            if (string.Equals(folderName, "INBOX", StringComparison.OrdinalIgnoreCase))
                folder = await _client!.Inbox.OpenAsync(FolderAccess.ReadOnly).ConfigureAwait(false);
            else
            {
                folder = await _client!.Inbox.GetSubfolderAsync(folderName).ConfigureAwait(false);
                await folder.OpenAsync(FolderAccess.ReadOnly).ConfigureAwait(false);
            }
            var uids = await folder.SearchAsync(SearchQuery.NotSeen).ConfigureAwait(false);
            if (uids.Count == 0)
                return new List<MailSummaryItem>();
            var take = Math.Min(maxCount, uids.Count);
            var lastUids = uids.TakeLast(take).ToArray();
            var summaries = await folder.FetchAsync(lastUids, MessageSummaryItems.Envelope | MessageSummaryItems.InternalDate).ConfigureAwait(false);
            var result = new List<MailSummaryItem>();
            foreach (var s in summaries.OrderByDescending(x => x.InternalDate))
            {
                var env = s.Envelope;
                var from = env?.From?.ToString() ?? "";
                if (from.Length > 50) from = from.Substring(0, 47) + "...";
                result.Add(new MailSummaryItem
                {
                    Sender = from,
                    Subject = env?.Subject ?? "",
                    Date = s.InternalDate.DateTime
                });
            }
            return result;
        }

        private void EnsureConnected()
        {
            lock (_clientLock)
            {
                if (_client == null || !_client.IsConnected)
                    throw new InvalidOperationException("IMAP client is not connected. Call ConnectAsync first.");
            }
        }
    }
}
