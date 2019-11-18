# OneDrive Photo Sorter

## Purpose

Provide tools for bulk management of OneDrive library contents, specifically to address challenges in OneDrive's automatic photo backup feature.

## Background

I've been using OneDrive iOS's photo backup feature for a few years. Overall, this feature is amazing. But, I've got two challenges:

1. Microsoft recently added the option to automatically sort photos into folders by month. This is great, but all my photos uploaded previously aren't sorted.
2. OneDrive sucks in *all* my photos, regardless of source. Family and friends share a lot of content on WhatsApp, and I'm glad to have this persisted to OneDrive, but I want to separate this content from *my* photos. Ultimately, I probably will upload *my* photos to Lightroom to manage with the stuff I shoot with my DSLR.

I initially tried to address issue #1 manually ... but the OneDrive web UX isn't really great for scrolling through long lists of photos. I could have synced these folders to my machine, and sorted manually there, but that's still a bit of manual work. Plus, differentiating WhatsApp photos from *my* photos is especially time consuming ... and *probably* something the computer can do for me, if I teach it a few rules.

## Core Heuristics

It turns out WhatsApp (in the spirit of using less data?!) strips EXIF data from photos. But, OneDrive preserves it for photos I've taken on my device. So, based the presence (or absence) of EXIF data, I can identify what photos I took myself.

This is more difficult with videos. Others' videos also shot on iPhone still seem to have the `photo` facet with a value set for `takenDateTime`. No details on camera make/model come through on the `photo` facet. So, for now, I've left my videos in the Camera Roll folder. I've got far fewer videos, so one option would be to move those out into their own hierarchy / a single folder.

## Caveats

To get things working quickly, I used a lot of brute-force techniques. Lots of opportunities to improve; don't judge. I promise I write better code when it *actually* goes to production. I've noted some possible opportunities for cleanup / generalization inline.

This code isn't really meant to be run as an app directly. I code up what I need using the methods given, and compile, and run the code. The overhead of making this an interactive app or anything was more than I wanted to take on.

## Acknowledgements

I always get hung up on auth - which library to use, where to register apps (apparently an MSA account is no longer sufficient for registering a converged app!), etc. These repos helped me figure out auth and the Microsoft Graph SDK:
* https://github.com/Azure-Samples/active-directory-dotnetcore-daemon-v2/
* https://github.com/OneDrive/graph-sample-photobrowser-uwp
