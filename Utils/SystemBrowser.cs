using Duende.IdentityModel.OidcClient.Browser;
using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace LMFS.Utils
{
    public class SystemBrowser : IBrowser
    {
        private readonly string _path;

        public SystemBrowser(string path = null)
        {
            _path = path;
        }


        public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:7890/");
            listener.Start();

            try
            {
                Process.Start(new ProcessStartInfo(options.StartUrl) { UseShellExecute = true });

                var context = await listener.GetContextAsync();

                //System.Diagnostics.Debug.WriteLine("A 지점: 컨텍스트 수신 성공");

                var responseString = "<html><head><title>인증 완료</title></head><body><script>window.close();</script></body></html>";
                var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

                var response = context.Response;
                response.ContentLength64 = buffer.Length;
                response.ContentType = "text/html; charset=utf-8";

                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();

                //  1초 딜레이
                await Task.Delay(500);

                return new BrowserResult
                {
                    ResultType = BrowserResultType.Success,
                    Response = context.Request.RawUrl
                };
            }
            catch (Exception ex)
            {
                return new BrowserResult
                {
                    ResultType = BrowserResultType.UnknownError,
                    Error = ex.Message
                };
            }
            finally
            {
                listener.Stop();
            }
        }



        // OS에 맞게 브라우저를 여는 헬퍼 메서드
        public static void OpenBrowser(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }

    }
}
