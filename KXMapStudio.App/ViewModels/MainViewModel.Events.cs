﻿using KXMapStudio.App.Actions;
using KXMapStudio.App.Services;
using KXMapStudio.App.State;

using System.Collections.Specialized;

namespace KXMapStudio.App.ViewModels;

public partial class MainViewModel
{
    private void WireEvents()
    {
        PackState.ActiveDocumentMarkers.CollectionChanged += OnActiveDocumentMarkersChanged;
        PackState.PropertyChanged += OnPackStateChanged;
        PackState.SelectedMarkers.CollectionChanged += OnSelectedMarkersChanged;
        _historyService.PropertyChanged += OnHistoryChanged;
        MumbleService.PropertyChanged += OnMumbleServiceChanged;
        _mapDataService.MapDataRefreshed += OnMapDataRefreshed;
    }

    private void OnPackStateChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(IPackStateService.IsWorkspaceLoaded):
            case nameof(IPackStateService.HasUnsavedChanges):
            case nameof(IPackStateService.IsWorkspaceArchive):
                OnPropertyChanged(nameof(Title));
                CloseWorkspaceCommand.NotifyCanExecuteChanged();
                SaveDocumentCommand.NotifyCanExecuteChanged();
                SaveAsCommand.NotifyCanExecuteChanged();
                break;
            case nameof(IPackStateService.ActiveDocumentPath):
                OnPropertyChanged(nameof(Title));
                UpdateMarkersInView();
                AddMarkerFromGameCommand.NotifyCanExecuteChanged();
                break;
            case nameof(IPackStateService.SelectedCategory):
                UpdateMarkersInView();
                break;
        }
    }

    private void OnSelectedMarkersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        CopySelectedMarkerGuidCommand.NotifyCanExecuteChanged();
        MoveSelectedMarkersUpCommand.NotifyCanExecuteChanged();
        MoveSelectedMarkersDownCommand.NotifyCanExecuteChanged();
    }

    private void OnHistoryChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HistoryService.CanUndo))
        {
            UndoCommand.NotifyCanExecuteChanged();
        }
        else if (e.PropertyName == nameof(HistoryService.CanRedo))
        {
            RedoCommand.NotifyCanExecuteChanged();
        }
    }

    private void OnMumbleServiceChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MumbleService.IsAvailable))
        {
            AddMarkerFromGameCommand.NotifyCanExecuteChanged();
        }
        if (e.PropertyName == nameof(MumbleService.CurrentMapId) || e.PropertyName == nameof(MumbleService.IsAvailable))
        {
            if (MumbleService.IsAvailable)
            {
                LiveMapName = _mapDataService.GetMapData((int)MumbleService.CurrentMapId)?.Name ?? "N/A";
            }
            else
            {
                LiveMapName = "N/A";
            }
        }
    }

    private void OnMapDataRefreshed()
    {
        // Force a refresh of the live map name when data arrives
        OnMumbleServiceChanged(this, new(nameof(MumbleService.CurrentMapId)));

        // Force the grid to re-evaluate the converter bindings
        // A simple way is to "reset" the collection which makes the UI redraw
        var markers = MarkersInView.ToList();
        MarkersInView.Clear();
        foreach (var marker in markers)
        {
            MarkersInView.Add(marker);
        }
    }

    private void OnActiveDocumentMarkersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateMarkersInView();
    }

    private void TryUndoLastAddMarker()
    {
        if (_historyService.CanUndo && _historyService.PeekLastActionType() == ActionType.AddMarker)
        {
            _historyService.Undo();
            _feedbackService.ShowMessage("Undid last marker addition via hotkey.");
        }
        else
        {
            _feedbackService.ShowMessage("Cannot undo: last action was not a marker addition.", actionContent: "OK");
        }
    }
}
