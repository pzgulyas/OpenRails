.. _start:

***************
Getting Started
***************

After having successfully installed Open Rails (see the Installation 
Manual), to run the game you must double-click on the Open Rails icon on 
the desktop, or on the OpenRails.exe file.

The OpenRails main window will appear. This displays your available MSTS 
installation profiles.

.. image:: images/start-activity.png

Installation Profiles
=====================

In the simplest case, where you have only a basic MSTS installation (see 
paragraph *Does Open Rails need MSTS to run?* for a precise definition of 
a MSTS installation) OR should already correctly point to that 
installation. To check this, you should initially see under ``Installation 
Profile`` the string ``- Default -``. Under ``Route`` you should see the 
name of one of the MSTS routes in your MSTS installation.

You can easily add, remove or move other MSTS installations and select 
among them (e.g. if you have any so-called ``mini-routes`` installed.). 
Click on the ``Options`` button and select the ``Content`` tab. See the 
``Content Options`` discussed below for more instructions.

.. _updating-or:

Updating OR
===========

When a new release of OR is available and your computer is online, a link 
``Update to xnnnn`` appears in the upper right corner. The string ``xnnnn`` is 
the release number of the newest release that matches your selected level 
of update. Various level of updates called Update Channels are available. 
You may choose the desired level in the ``Options-Update`` window, described 
:ref:`below <options-updater>`.

When you click on the update link OR will download and install the new 
release. In this way your version of Open Rails is always up to date. 
Note, however, that previously saved games may not be compatible with 
newer versions, as described here.

Clicking the link ``What's new?`` in the upper centre part of the main menu 
window will connect to a website that summarizes the most recent changes 
to the OR program.

Further General Buttons
=======================

Tools
-----

By clicking this button you get access to the ancillary tools (see :ref:`here 
<intro-reality>`).

Documents
---------

This button becomes selectable only if you have at least once updated to a 
testing version or to a stable version greater than 1.0. By clicking this 
button you get immediate access to the OR documentation.

Preliminary Selections
----------------------

Firstly, under ``Route:`` select the route on which you wish to run.

If you check the ``Logging`` checkbox, Open Rails will generate a log file 
named ``OpenRailsLog.txt`` that resides on your desktop. This log file is very 
useful to document and investigate malfunctions.

At every restart of the game (that is, after clicking ``Start`` or ``Server`` 
or ``Client``) the log file is cleared and a new one is generated.

If the ``Windowed`` checkbox is checked, Open Rails will run in a window 
instead of full screen.

If you wish to fine-tune Open Rails for your system, click on the 
``Options`` button. See the Chapter: ``Open Rails Options`` which describes 
the extensive set of OR options. It is recommended that you read this 
chapter.

Gaming Modes
============

One of the plus points of Open Rails is the variety of gaming modes you 
can select.

Traditional Activity and Explore modes
--------------------------------------

As a default you will find the radio button ``Activity`` selected in the 
start window, as above.

This will allow you to run an activity or run in explore mode.

If you select ``-Explore Route-`` (first entry under ``Activity:``), you will 
also have to select the consist, the path, the starting time, the season 
and the weather with the relevant buttons.

To select the consist you have two possibilities: either you click under 
``Consist:``, and the whole list of available consists will appear, or you 
first click under ``Locomotive:``, where you can select the desired 
locomotive, and then click under ``Consist:``, where only the consists led 
by that locomotive will appear.

If you instead select a specific activity, you won't have to perform any 
further selections.

If you have selected the related Experimental Option, at runtime you can 
switch Autopilot mode on or off, which allows you to watch OR driving your 
train, as if you were a trainspotter or a visitor in the cab. 

.. _start-timetable:

Timetable Mode
--------------

If you select the radio button ``Timetable``, the main menu window will 
change as follows:

.. image:: images/start-timetable.png

Timetable mode is unique to Open Rails, and is based on a ``timetable`` that 
is created in a spreadsheet formatted in a predefined way, defining trains 
and their timetables, their paths, their consists, some operations to be 
done at the end of the train run, and some train synchronization rules.

Timetable mode significantly reduces development time with respect to 
activities in cases where no specific shunting or train operation is 
foreseen. The complete description of the timetable mode can be found here.

The spreadsheet has a .csv format, but it must be saved in Unicode format 
with the extension ``.timetable_or`` in a subdirectory named ``Openrails`` 
that must be created in the route's ``ACTIVITIES`` directory. 

For the game player, one of the most interesting features of timetable 
mode is that any one of the trains defined in the timetable can be 
selected as the player train.

The drop-down window ``Timetable set:`` allows you to select a timetable 
file from among those found in the route's ``Activities/Openrails/`` folder.

Now you can select in the drop-down window ``Train:`` from all of the trains 
of the timetable the train you desire to run as the Player train. Season 
and weather can also be selected.

Run!
----

Now, click on ``Start``, and OR will start loading the data needed for your 
game. When loading completes you will be within the cab of your 
locomotive! You can read further in the chapter ``Driving a Train``.

Multiplayer Mode
----------------

Open Rails also features this exciting game mode: several players, each 
one on a different computer in a local network or through the Internet, 
can play together, each driving a train and seeing the trains of the other 
players, even interacting with them by exchanging wagons, under the 
supervision of a player that acts as dispatcher. The multiplayer mode is 
described in detail here.

Replay
------

This is not a real gaming mode, but it is nevertheless another way to 
experience OR. After having run a game you can save it and replay it: OR 
will save all the commands that you gave, and will automatically execute 
the  commands during replay: it's like you are seeing a video on how you 
played the game. Replay is described later together with the save and 
resume functions.
