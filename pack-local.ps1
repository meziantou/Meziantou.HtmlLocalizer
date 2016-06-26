$Suffix = "beta-" + [datetime]::UtcNow.ToString("yyyyMMddhhmmss")

dotnet pack .\src\Meziantou.HtmlLocalizer -o c:\nuget --version-suffix $Suffix
dotnet pack .\src\Meziantou.HtmlLocalizer.Tools -o c:\nuget --version-suffix $Suffix