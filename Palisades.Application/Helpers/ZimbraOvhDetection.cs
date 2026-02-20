using System;
using System.Text.RegularExpressions;

namespace Palisades.Helpers
{
    /// <summary>
    /// Détection / pré-remplissage pour configuration Zimbra OVH (Phase 7.3).
    /// À partir de l'adresse email, suggère serveur IMAP et URL CalDAV selon les conventions OVH.
    /// </summary>
    public static class ZimbraOvhDetection
    {
        private static readonly Regex EmailRegex = new Regex(@"^[^@]+@(.+)$", RegexOptions.Compiled);

        /// <summary>
        /// Suggère l'hôte IMAP et l'URL de base CalDAV pour un email (conventions OVH Zimbra).
        /// L'utilisateur peut ajuster manuellement.
        /// </summary>
        public static (string ImapHost, string CalDAVBaseUrl) SuggestFromEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return ("ssl0.ovh.net", "");

            email = email.Trim();
            var match = EmailRegex.Match(email);
            var domain = match.Success ? match.Groups[1].Value : "";

            // OVH : souvent ssl0.ovh.net pour IMAP, et https://ssl0.ovh.net/dav/email@domain/ pour CalDAV
            // Certains domaines ont un serveur dédié type mail.domaine.com
            string imapHost = "ssl0.ovh.net";
            string hostForCalDav = "ssl0.ovh.net";
            if (domain.EndsWith(".ovh", StringComparison.OrdinalIgnoreCase) ||
                domain.EndsWith(".ovh.net", StringComparison.OrdinalIgnoreCase))
            {
                // Garder ssl0.ovh.net
            }
            else if (!string.IsNullOrEmpty(domain))
            {
                // Option : mail.domaine.com pour les domaines personnalisés OVH
                hostForCalDav = "mail." + domain;
                imapHost = "mail." + domain;
            }

            var caldavBaseUrl = $"https://{hostForCalDav}/dav/{email}/";
            return (imapHost, caldavBaseUrl);
        }
    }
}
