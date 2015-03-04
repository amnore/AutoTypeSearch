AutoTypeSearch
==============
http://sourceforge.net/projects/autotypesearch


This is a plugin to KeePass <http://www.KeePass.info> to provide a quick searching capability as
an enhancement to the global auto-type system. If a global auto-type is requested, but no matching
entry for the active window is found, this plugin will show a quick as-you-type search window which
lets you to easily pick the entry to auto-type.


Installation
------------
Place AutoTypeSearch.plgx in your KeePass Plugins folder.


Usage
-----
AutoTypeSearch is initially configured to automatically appear after an unsuccessful global
auto-type. However, this can be changed in the KeePass Options window (an AutoTypeShow tab has
been added). Here, a system-wide hot key can be configured to show the AutoTypeSearch window
immediately. It is also possible to show the window by running KeePass.exe passing "/e:AutoTypeSearch"
as a command line parameter.

Once the window is shown, usage is extremely simple. Just start typing, and AutoTypeSearch will
search your database for matching entries. By default, the Title, Url, Notes, Tags, and Custom Fields
will be searched, but this can be configured in the AutoTypeShow tab of the KeePass Options window.

Protected fields (like Password) will not be searched.

The arrow keys can be used to move the selection in the list of results, then press Enter to auto-
type the selected entry. Alternatively, press Shift+Enter to open the entry instead of auto-typing it.
(These actions can also be customised in the Options window.) Clicking and Shift-Clicking an entry will
also perform those actions.


Uninstallation
--------------
Delete AutoTypeSearch.plgx from your KeePass Plugins folder.


Checking for updates
--------------------
If you want to use the KeePass Check for Updates function to check for updates to this plugin
then it requires the SourceForgeUpdateChecker plugin to be installed too:
http://sourceforge.net/projects/kpsfupdatechecker


Bug Reporting, Questions, Comments, Feedback, Donations
-------------------------------------------------------
Please use the SourceForge project page: <http://sourceforge.net/projects/autotypesearch>
Bugs can be reported using the issue tracker, for anything else, a discussion forum is available.


Changelog
---------
v0.1
 Initial release

v0.2
 Added information banner when search is shown as a result of an unsuccessful global auto-type
 Compatibility with Linux/Mono

v0.3
 Added search result prioritisation for entries where the match is found at the start of the field

v0.4
 Added support for multiple databases. All currently open, unlocked, databases will be searched


Attributions
------------
Throbber image by FlipDarius http://www.mediawiki.org/wiki/File:Loading.gif