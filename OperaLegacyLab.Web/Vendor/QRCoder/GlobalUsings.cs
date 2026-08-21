// Not part of upstream QRCoder - see README.md in this folder. Upstream normally supplies these
// via its own Directory.Build.props; this project has no equivalent shared file, so they're
// declared here instead, scoped to nothing in particular (global usings apply project-wide, same
// as upstream's own build - this project just has no other file that would collide with them).
global using BitArray = System.Collections.BitArray;
global using System.Globalization;
global using System.Text;
global using System.Text.RegularExpressions;
