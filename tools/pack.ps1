$csproj = "$PSScriptRoot\..\src\NuExt.Minimal.Mvvm.Wpf.csproj"
$Configuration = "Release"
$outDir = $PSScriptRoot

dotnet clean $csproj -c $Configuration
dotnet pack $csproj -c $Configuration -o $outDir