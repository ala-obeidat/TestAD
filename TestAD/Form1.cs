using System;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.Protocols;
using System.IdentityModel.Protocols.WSTrust;
using System.IdentityModel.Tokens;
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Security;
using System.Windows.Forms;

namespace TestAD
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (slDap.Checked)
                {
                    var connection = new LdapConnection(new LdapDirectoryIdentifier(DomainTxt.Text, Convert.ToInt32(portNumberTxt.Text)));
                    connection.SessionOptions.SecureSocketLayer = true;
                    connection.SessionOptions.ProtocolVersion = 3;
                    connection.AuthType = AuthType.Negotiate;
                    connection.SessionOptions.VerifyServerCertificate = new VerifyServerCertificateCallback((con, cer) => true);
                    connection.Bind(new NetworkCredential(UsernameTxt.Text, PasswordTxt.Text, DomainTxt.Text));
                }
                else
                {
                    DirectoryEntry entry = new DirectoryEntry("LDAP://" + DomainTxt.Text, UsernameTxt.Text, PasswordTxt.Text);
                    object nativeObject = entry.NativeObject;
                }
                MessageBox.Show("Success", "Authenticated");
            }
            catch (Exception ex)
            {
                if (slDap.Checked && ex.Message.Equals("The supplied credential is invalid."))
                    MessageBox.Show(ex.Message + " ( Password or username not correct ) ", "Faild");
                else
                    MessageBox.Show(ex.Message, "Exception");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var binding = new WS2007HttpBinding(SecurityMode.TransportWithMessageCredential);

            // If you set this to true, you can use a Security Context Token (SCT) for your next
            // calls to the STS instead of sending a Username and Password each time.
            binding.Security.Message.EstablishSecurityContext = false;

            // Instead of using Username/Password authentication you can use any of the 
            // other ClientCredentialType's such as Certificate, IssuedToken or Windows.
            binding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;

            // The same applies to the transport level security. You can secure it in any 
            // of the available ways.
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;

            var endpoint = new EndpointAddress(endPontTxt.Text);
            var factory = new WSTrustChannelFactory(binding, endpoint);
            factory.TrustVersion = TrustVersion.WSTrust13;
            factory.Credentials.SupportInteractive = false;

            // If your MessageCredentialType differs from UserName, make sure you supply 
            // the valid credential here.
            factory.Credentials.UserName.UserName = username_ADFS.Text;
            factory.Credentials.UserName.Password = password_ADFS.Text;

            var rst = new RequestSecurityToken()
            {
                RequestType = RequestTypes.Issue,
                KeyType = KeyTypes.Bearer,
                AppliesTo = new EndpointReference(relyTxt.Text),
                TokenType = "urn:oasis:names:tc:SAML:2.0:assertion"
            };

            var channel = (WSTrustChannel)factory.CreateChannel();
            RequestSecurityTokenResponse rstr;
            try
            {
                SecurityToken token = channel.Issue(rst, out rstr);
                MessageBox.Show("Success", "Authenticated");

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                var contextType = ContextType.Domain;
                if (radioButton1.Checked)
                    contextType = ContextType.Machine;
                else if (radioButton2.Checked)
                    contextType = ContextType.Domain;
                else if (radioButton3.Checked)
                    contextType = ContextType.ApplicationDirectory;
                else
                    MessageBox.Show("Please Check Context Type");

                var loginName = string.Empty;
                using (var pc = new PrincipalContext(contextType, DomainTxt.Text, UsernameTxt.Text, PasswordTxt.Text))
                {
                    using (var searcher = new PrincipalSearcher(new UserPrincipal(pc)))
                    {
                        foreach (var item in searcher.FindAll())
                        {
                            var de = item.GetUnderlyingObject() as DirectoryEntry;
                            //Console.WriteLine("First Name : " + de.Properties["givenName"].Value);
                            loginName = de.Properties["samAccountName"].Value.ToString();
                        }
                    }
                }

                MessageBox.Show("This user is in AD", loginName);
                return;
                using (var domainContext = new PrincipalContext(contextType, DomainTxt.Text, UsernameTxt.Text, PasswordTxt.Text))
                {
                    using (var foundUser = UserPrincipal.FindByIdentity(domainContext, IdentityType.SamAccountName, usernameToCheckTxt.Text))
                    {
                        if (foundUser != null)
                            MessageBox.Show("This user is in AD", "Success");
                        else
                            MessageBox.Show("This user dose not exists in AD", "Fail");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception");
            }
        }
    }

}
