# Palisades

<p align="center">
  <a href="https://github.com/Emilien-Etadam/Palisades-Navigation/blob/main/LICENSE">
    <img alt="mit" src="https://img.shields.io/github/license/Emilien-Etadam/Palisades-Navigation?style=for-the-badge"/>
  </a>
  <a href="https://github.com/Emilien-Etadam/Palisades-Navigation/releases">
    <img alt="mit" src="https://img.shields.io/github/v/release/Emilien-Etadam/Palisades-Navigation?label=Version&style=for-the-badge"/>
  </a>
  <a href="https://github.com/Emilien-Etadam/Palisades-Navigation/releases">
    <img alt="mit" src="https://img.shields.io/github/downloads/Emilien-Etadam/Palisades-Navigation/total?style=for-the-badge"/>
  </a>
</p>

## Introduction

Palisades lets you organize your desktop with small, always-on-bottom windows (palisades) that can hold shortcuts, act as mini file explorers, show CalDAV tasks or calendars, and display unread mail counts. Palisades stay behind your windows and can be grouped in tabs or saved/restored as layouts.

## Getting Started

Just download the latest installer on the [Releases](https://github.com/Emilien-Etadam/Palisades-Navigation/releases) page, use it to install Palisades and run the software.

## Features

- **Standard Palisades**: Drag and drop shortcuts; reorder by drag and drop; customize name, header/body colors, and text colors.
- **Folder Portals**: Mini file explorer for a chosen folder on the desktop (breadcrumbs, open with default app).
- **Task Palisade**: CalDAV task list (e.g. Zimbra), sync, add/edit/complete/delete tasks.
- **Calendar Palisade**: CalDAV calendars, agenda view, multiple calendars.
- **Mail Palisade**: IMAP unread count and optional subject list (e.g. Zimbra), configurable polling.
- **Create by drawing**: Right-click + drag on the desktop to draw a rectangle, then choose the palisade type to create at that size and position.
- **Tabs**: Palisades can be grouped into a single tabbed window; load/save preserves groups.
- **Layouts**: Save the current set of palisades as a named layout, restore later (with position rescaling if resolution changed). Up to 5 recent layouts in the context menu; full manage dialog (rename, delete, export, import). Auto-save on exit keeps the last 3 auto-saved layouts.
- **Zimbra / OVH**: Central account management (CalDAV + IMAP), encrypted credentials (DPAPI), optional auto-detect from email.

## Usage

- **Shortcuts**: Drag and drop into a Standard Palisade.
- **New palisade**: Right-click a palisade header for the full menu, or right-click + drag on the desktop to draw a new one.
- **Layouts**: Right-click a palisade header → Layouts → Save current layout… or Manage layouts….

## Tech

.NET 8, WPF. Material Design In XAML for parts of the UI; Sentry for error reporting. Inspired by [Twometer's NoFences](https://github.com/Twometer/NoFences) and [Stardock's Fences](https://www.stardock.com/products/fences/).
