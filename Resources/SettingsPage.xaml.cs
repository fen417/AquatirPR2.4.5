using Microsoft.Maui.Storage;
using System.Net.Mail;
using System.Net;

namespace Aquatir
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();
            AutoReturnSwitch.IsToggled = Preferences.Get("AutoReturnEnabled", false);
            //  ShowPriceSwitch.IsToggled = Preferences.Get("ShowPriceEnabled", false);
            ShowPackagedProductsSwitch.IsToggled = Preferences.Get("ShowPackagedProducts", true); // �� ��������� ��������
            IgnoreColorsSwitch.IsToggled = Preferences.Get("IgnoreColors", false);
            // �������� �� ������� ��������������
            AutoReturnSwitch.Toggled += OnAutoReturnToggled;
            IgnoreColorsSwitch.Toggled += OnIgnoreColorsToggled;
            //  ShowPriceSwitch.Toggled += OnShowPriceToggled;
            ShowPackagedProductsSwitch.Toggled += OnShowPackagedProductsToggled;
        }

        private void OnAutoReturnToggled(object sender, ToggledEventArgs e)
        {
            Preferences.Set("AutoReturnEnabled", e.Value);
        }

        private void OnIgnoreColorsToggled(object sender, ToggledEventArgs e)
        {
            Preferences.Set("IgnoreColors", e.Value);
        }

        /*  private void OnShowPriceToggled(object sender, ToggledEventArgs e)
          {
              Preferences.Set("ShowPriceEnabled", e.Value);
          } */

        private void OnShowPackagedProductsToggled(object sender, ToggledEventArgs e)
        {
            Preferences.Set("ShowPackagedProducts", e.Value);
            // �������� ���������� ���������
            if (Application.Current.MainPage is MainPage mainPage)
            {
                mainPage.ReloadProducts();
            }
        }

        private async void OnWhatsNewClicked(object sender, EventArgs e)
        {
            string patchNotes = @"
��� ������ � ������ r2.8.1:
- ��������� ��������� �����������
- ��������� ��������� �������� �����
- ���������� ������
��� ������ � ������ r2.8.0:
- ������������ ����������� �������� �������� ����� ��� ������ ��������� ���� ������
- �������� ������� '���. ������'
- ���������� ���� ��������� ��� ������ ��������-���������� ��� �� ����� �������
��� ������ � ������ r2.7.0:
- ��������� ������ '��� ���������', ���������� ������ ������ ���������
- ��������� ���������� 
��� ������ � ������ r2.6.2:
- ����������� ������ � ����������� ����������
��� ������ � ������ r2.6.1:
- ������ � ������������� ������ �������� ������ ���������
��� ������ � ������ r2.6.0:
- ��������� push-����������� � ������������ � ������ �������� �������� (������ �� � 17:00)
- �������� ������������������ ��� �������� ������� ������� �������
- ���������� ������ ������ � �������� ������������ ����������
- ������� ��������� ��������
��� ������ � ������ r2.5.3:
- ��������� �������� ���� ������
- ������������� ���
- ���������� ������ ������ (Hotfix #1)
- ���������� ������ ������ (Hotfix #2)
��� ������ � ������ r2.5.0:
- ���������� ���������� ���������, ���������� �������� ����
- ���������� ������������ ��������� � ������ � �������������
";
            await DisplayAlert("��� ������?", patchNotes, "��");
        }

        private async void OnReportIssueClicked(object sender, EventArgs e)
        {
            string userInput = await DisplayPromptAsync(
                "��������� �� ������ ��� �����������",
                "����������, ������� ������������ ������ ��� ���������� ���������:",
                "���������",
                "������",
                placeholder: "������� ����� ���������");

            if (!string.IsNullOrWhiteSpace(userInput))
            {
                await SendFeedbackEmail(userInput);
            }
        }

        private async Task SendFeedbackEmail(string feedbackText)
        {
            try
            {
                var message = new MailMessage();
                message.From = new MailAddress("rep.1958@mail.ru", "Aquatir App");
                message.To.Add(new MailAddress("rep.1958@mail.ru", "��������� Aquatir"));
                message.Subject = "�������� ����� �� ���������� Aquatir";
                message.Body = $"�������� ����� �� ������������ ����������:\n\n{feedbackText}\n\n2.8.1";

                using (var client = new SmtpClient("smtp.mail.ru", 587)) // ������� ���� �� 587
                {
                    client.EnableSsl = true;
                    client.UseDefaultCredentials = false; // ���� ��������� �� ������������ ������� ������ �� ���������
                    client.Credentials = new NetworkCredential("rep.1958@mail.ru", "zyxrhkQb4KwE0Udwz2cx");
                    client.Timeout = 10000; // ��������� �������� � 10 ������

                    try
                    {
                        // ���������� ����������� ����� SendMailAsync ������ Task.Run
                        await client.SendMailAsync(message);
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                            await DisplayAlert("�������", "���� ��������� ������� ����������. ������� �� �������� �����!", "OK"));
                    }
                    catch (Exception ex)
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                            await DisplayAlert("������", $"�� ������� ��������� ���������: {ex.Message}", "OK"));
                    }
                }
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                    await DisplayAlert("������", $"��������� ������: {ex.Message}", "OK"));
            }
        }
    }
}