﻿#pragma checksum "..\..\..\..\UserWindows\RenamePlaylistWindow.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "678D423F7B65596A7CA97E2C3BD9251CBC586A2D"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Client_Application;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace Client_Application {
    
    
    /// <summary>
    /// RenamePlaylistWindow
    /// </summary>
    public partial class RenamePlaylistWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 10 "..\..\..\..\UserWindows\RenamePlaylistWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox newPlaylistNameTextBox;
        
        #line default
        #line hidden
        
        
        #line 11 "..\..\..\..\UserWindows\RenamePlaylistWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button renamePlaylistButton;
        
        #line default
        #line hidden
        
        
        #line 12 "..\..\..\..\UserWindows\RenamePlaylistWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button cancelRenamePlaylistButton;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "6.0.8.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/Client Application;V1.0.0.0;component/userwindows/renameplaylistwindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\..\UserWindows\RenamePlaylistWindow.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "6.0.8.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.newPlaylistNameTextBox = ((System.Windows.Controls.TextBox)(target));
            return;
            case 2:
            this.renamePlaylistButton = ((System.Windows.Controls.Button)(target));
            
            #line 11 "..\..\..\..\UserWindows\RenamePlaylistWindow.xaml"
            this.renamePlaylistButton.Click += new System.Windows.RoutedEventHandler(this.renamePlaylistButton_Click);
            
            #line default
            #line hidden
            return;
            case 3:
            this.cancelRenamePlaylistButton = ((System.Windows.Controls.Button)(target));
            
            #line 12 "..\..\..\..\UserWindows\RenamePlaylistWindow.xaml"
            this.cancelRenamePlaylistButton.Click += new System.Windows.RoutedEventHandler(this.cancelRenamePlaylistButton_Click);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

