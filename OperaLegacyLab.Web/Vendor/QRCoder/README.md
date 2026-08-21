# Vendored QRCoder (core encoder only)

These files are copied, unmodified, from the [QRCoder](https://github.com/codebude/QRCoder)
library (MIT license, see `LICENSE-QRCoder.txt`), commit `544a0be1bfc75a7007d8039e3ec987127b8709dc`
(post-1.6.0 `main`, the same major version already used by the separate `ResidentPass.MAUI`
project this lab's `/test/qr` page is modeled on).

Only the core QR-matrix encoder is included - `QRCodeGenerator` (+ its `QRCodeGenerator/` helper
folder: Galois field math, Reed-Solomon polynomial division, alphanumeric/byte/numeric segment
encoding, mask-pattern selection, module placement) and `QRCodeData` (the plain `List<BitArray>`
module matrix result), plus the small set of things those two need to compile: the exception type
`CreateQrCode` throws on oversized input (`Exceptions/DataTooLongException.cs`), one `BitArray`
extension method used during codeword interleaving (`Extensions/BitArrayExtensions.cs`), and the
abstract base type referenced by two unused `CreateQrCode` overloads that take a
`PayloadGenerator.Payload` (`PayloadGenerator/Payload.cs` - only the base class, none of the ~20
concrete payload types like `WiFi`/`Girocode`/`SwissQrCode` that live alongside it upstream). None
of QRCoder's *rendering* classes are included
(`SvgQRCode`, `PngByteQRCode`, `ArtQRCode`, etc.) - this app renders the module matrix itself as a
plain HTML `<table>` (`Infrastructure/HtmlTableQrRenderer.cs`), since none of QRCoder's own
renderers produce markup an Opera 6-era browser can display (SVG/PNG/canvas are all no-gos here -
see the reasoning in `/test/qr`).

## Why vendored instead of a NuGet `PackageReference`

This app is normally built directly from source without a NuGet restore step available (the
sandbox this was authored in only had network access to source hosts, not nuget.org) - vendoring
the handful of dependency-free encoder files sidesteps that entirely and lets `/test/qr` be
built, run, and verified end-to-end the same way every other test in this lab was. There is
nothing OperaLegacyLab-specific about the vendored code itself; if NuGet access is available in
your own environment, swapping this folder back out for `<PackageReference Include="QRCoder" />`
(as `ResidentPass.MAUI/QrCodeGenerator/QRCodeGenerator.csproj` already does) works as a drop-in
replacement - the public API (`QRCodeGenerator.CreateQrCode(string, ECCLevel)` →
`QRCodeData.ModuleMatrix`) is unchanged.

## Modification note

Only change made to any file here: `GlobalUsings.cs` (new, not part of upstream QRCoder) declares
the global usings upstream's own `Directory.Build.props` normally supplies project-wide
(`BitArray`, `System.Globalization`, `System.Text`, `System.Text.RegularExpressions`) - this repo
has no such shared build-props file, so those need to be declared somewhere for the vendored code
to compile as-is.
