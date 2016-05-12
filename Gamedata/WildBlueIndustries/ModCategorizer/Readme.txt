Mod Categorizer

A category creator for KSP. Because Squad didn't make it easy.

Usage

This Modlet will create a custom category for you quickly and efficiently. It will search through all the folders that you specify. If it finds a part in one of the specified folders, then it will add it to the category. The mod is smart, any subfolders containing parts will also be added.

Be sure to create an icon for your category. I typically use 32x32.

DO NOT USE THIS IF YOU HAVE WILDBLUETOOLS! It has its built-in part categorizer (generously donated by WildBlueTools).

Create your own custom category.cfg file and paste the following MODCAT node. Below is an example.

MODCAT
{
	//Name of the folder(s) to look for parts. Separate multiple directories with a semicolon	
	folderName = MOLE/Parts

	//This is the icon to use when the category is just sitting around not being selected.
	normalPath = WildBlueIndustries/MOLE/Icons/MoleIcon

	//This is the icon to use when the category has been selected
	selectedPath = WildBlueIndustries/MOLE/Icons/MoleIcon

	//This is the title of your category
	title = Mark One Laboratory Extensions
}

---LICENSE---

Source code copyrighgt 2014, by Michael Billard (Angel-125)
License: CC BY-NC-SA 4.0
License URL: https://creativecommons.org/licenses/by-nc-sa/4.0/
Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.