   M S B A M L   `   `   `  ????M  IClient Application, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null7 clr-namespace:Client_ApplicationClient_Application  5   	   *    $Client_Application.AddPlaylistWindow   5   	   W SPresentationCore, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   2local clr-namespace:Client_Application     5   	   -   5        $??addNewPlaylist??5   	     ? Title$  Create New Playlist??6    $	??150??6<   $	??300??6I    ? WindowStartupLocation$ CenterScreen=?6U    ? 
ResizeMode$ NoResize=?6z   .??5	      ? .+?5
   
   ?? -   5         ??Name$ newPlaylistTextbox??5
      $
??Left=?6.   $??	78,36,0,0q?6I    ?? TextWrapping$
 Wrap=?6\   $	??Top=?6p   $	??148??6?   ?? 5   
   -   5         ??Name$ createNewPlaylistButton??5      $??Create??62   $
??Left=?6C   $??	94,83,0,0q?6^   $	??Top=?6q   $??40??6?   $??20??6?   6?   ?? 5   
   -   5        $# cancelCreateNewPlaylistButton??5      $??Cancel??68   $
??Left=?6I   $??
166,83,0,0q?6d   $	??Top=?6x   $??40??6?   $??20??6?   6?   5      5      