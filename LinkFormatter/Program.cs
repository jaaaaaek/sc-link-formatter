using System.Configuration;
namespace LinkFormatter
{
    internal class Program
    {
        static void Main()
        {
            try
            {
                string formattedLinks = FormatLinks();

                Console.Write("\n\n");
                Console.Write("Here are the formatted links! Paste them into the \"Music\" folder for them to be downloaded!");
                Console.Write("\n\n");
                Console.Write(formattedLinks);
                Console.Write("\n\n");
            }
            catch(Exception e)
            {
                Console.Write("\n\n");
                Console.Write("Error! " + e.Message);
                Console.Write("\n\n");
            }
        }

        static string FormatLinks()
        {
            string authToken = ConfigurationManager.AppSettings["SoundCloudToken"];
            string currentDir = Directory.GetCurrentDirectory();
            string returnme = string.Empty;

            try
            {
                if (string.IsNullOrWhiteSpace(authToken))
                {
                    throw new Exception("No \"SoundCloudToken\" value found in app.config!");
                }

                List<string> existingLinks = GenerateExistingLinks(currentDir);

                returnme = GenerateFormattedLinks(currentDir, authToken, existingLinks);
                return returnme;
                
            }
            catch (Exception e)
            {
                throw;
            }
        }
        static string GenerateFormattedLinks(string currentDir, string authToken, List<string> existingLinks)
        {
            string returnme = string.Empty;
            try
            {
                // Go through links in NewDownloads.txt and format them!
                StreamReader sr = new StreamReader(currentDir + "\\Music\\NewDownloads.txt");
                string lineNew = sr.ReadLine();
                if (string.IsNullOrWhiteSpace(lineNew))
                {
                    throw new Exception("No links found in NewDownloads.txt! Put some links from soundcloud in there!");
                }

                while (lineNew != null)
                {
                    // if link does not exist in existingLinks,
                    if (lineNew.Count(x => x.Equals('/')) != 3 &&              // isn't an album/artist link (three /'s),
                        !lineNew.Contains("\\you\\")                           // doesn't contain /you/,
                        && !existingLinks.Any(x => x.Equals(lineNew.Trim())))  // and doesn't have a match within all song names in target directory
                    {
                        // format to be returned. 
                        returnme += $"yt-dlp -f ba --extract-audio --audio-format wav {lineNew} --add-header \"Authorization: OAuth {authToken}\" --extractor-retries 10 --retry-sleep extractor:300\n";

                    }
                    lineNew = sr.ReadLine();
                }
                sr.Close();
                return returnme;
            }
            catch(Exception e)
            {
                throw;
            }
        }
        static List<string> GenerateExistingLinks(string currentDir)
        {
            List<string> returnme = new List<string>();
            try
            {
                // Go through ExistingDownloads.txt and put each line in the existingLinks collection
                StreamReader sr = new StreamReader(currentDir + "\\Music\\ExistingDownloads.txt");
                string lineExisting = sr.ReadLine();
                while (lineExisting != null)
                {
                    returnme.Add(lineExisting.Trim());
                    lineExisting = sr.ReadLine();
                }
                sr.Close();
                return returnme;
            }
            catch(Exception e)
            {
                throw;
            }
        }
    }
}