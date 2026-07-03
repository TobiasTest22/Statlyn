using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Statlyn.Data;
using Statlyn.Data.Scouting;
using Statlyn.Data.Shortlists;
using Statlyn.UnityApp.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statlyn.UnityApp.Pages
{
    public sealed class ShortlistsPageBuilder
    {
        public void Build(VisualElement main)
        {
            main.Clear();
            var databasePath = new StatlynDatabasePathResolver().ResolvePath(Application.persistentDataPath, StatlynDatabasePathMode.RuntimeMain);
            BuildHeader(main);

            var form = new VisualElement();
            form.AddToClassList("data-source-form");
            main.Add(form);

            var name = new TextField("Name");
            name.value = ShortlistWorkflowService.DefaultShortlistName;
            form.Add(name);
            var description = new TextField("Description");
            form.Add(description);

            var actions = new VisualElement();
            actions.AddToClassList("action-row");
            form.Add(actions);
            var create = new Button { text = "Create Shortlist" };
            var refresh = new Button { text = "Refresh" };
            actions.Add(create);
            actions.Add(refresh);

            var message = new Label(string.Empty);
            message.AddToClassList("card-row");
            form.Add(message);

            var overview = new VisualElement();
            overview.AddToClassList("data-source-results");
            main.Add(overview);
            var detail = new VisualElement();
            detail.AddToClassList("data-source-results");
            main.Add(detail);

            RenderShortlists(databasePath, overview, detail, message, 0);

            create.clicked += () =>
            {
                try
                {
                    using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                    {
                        var created = new ShortlistWorkflowService(factory).CreateShortlist(name.value, description.value);
                        message.text = "Created shortlist from persisted safe workflow data.";
                        RenderShortlists(databasePath, overview, detail, message, created.Id);
                    }
                }
                catch (Exception ex)
                {
                    message.text = "Could not create shortlist safely: " + ex.GetType().Name + ": " + ex.Message;
                }
            };

            refresh.clicked += () => RenderShortlists(databasePath, overview, detail, message, 0);
        }

        private static void BuildHeader(VisualElement main)
        {
            var header = new VisualElement();
            header.AddToClassList("header");
            main.Add(header);

            var headerBrand = new VisualElement();
            headerBrand.AddToClassList("header-brand");
            header.Add(headerBrand);
            var logo = StatlynUiFactory.MakeLogoImage(StatlynUiFactory.LightLogoResourceKey, "header-logo");
            if (logo != null)
            {
                headerBrand.Add(logo);
            }

            var titleStack = new VisualElement();
            titleStack.AddToClassList("title-stack");
            headerBrand.Add(titleStack);
            var title = new Label("Shortlists");
            title.AddToClassList("screen-title");
            titleStack.Add(title);
            var subtitle = new Label("Persisted safe recruitment decisions - no hidden data");
            subtitle.AddToClassList("screen-subtitle");
            titleStack.Add(subtitle);

            var status = new Label("Decision workflow");
            status.AddToClassList("status-pill");
            header.Add(status);
        }

        private static void RenderShortlists(string databasePath, VisualElement overview, VisualElement detail, Label message, long selectedShortlistId)
        {
            overview.Clear();
            detail.Clear();
            try
            {
                using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                {
                    var service = new ShortlistWorkflowService(factory);
                    var page = service.BuildPageViewModel(includeArchived: false);
                    message.text = string.IsNullOrWhiteSpace(message.text) ? page.SafeMessage : message.text;
                    RenderOverview(databasePath, page, overview, detail, message, selectedShortlistId);
                    if (page.Shortlists.Count == 0)
                    {
                        detail.Add(StatlynUiFactory.MakeCard("No shortlists yet", new[] { "Add players from Recruitment Centre or Player Profile.", "No fake players are shown." }));
                        return;
                    }

                    var selectedId = selectedShortlistId == 0 ? page.Shortlists[0].ShortlistId : selectedShortlistId;
                    RenderDetail(databasePath, service.BuildDetailViewModel(selectedId), detail, overview, message);
                }
            }
            catch (Exception ex)
            {
                overview.Add(StatlynUiFactory.MakeCard("Shortlists", new[] { "Could not load persisted shortlists safely.", ex.GetType().Name + ": " + ex.Message }));
            }
        }

        private static void RenderOverview(string databasePath, ShortlistsPageViewModel page, VisualElement overview, VisualElement detail, Label message, long selectedShortlistId)
        {
            var grid = new VisualElement();
            grid.AddToClassList("dashboard-grid");
            overview.Add(grid);

            if (page.Shortlists.Count == 0)
            {
                grid.Add(StatlynUiFactory.MakeCard("Empty State", new[] { "No shortlists yet. Add players from Recruitment Centre or Player Profile." }));
                return;
            }

            foreach (var shortlist in page.Shortlists)
            {
                var card = new VisualElement();
                card.AddToClassList("glass-card");
                card.Add(StatlynUiFactory.MakeSectionTitle(shortlist.Name));
                card.Add(new Label(shortlist.Description));
                card.Add(new Label("Players: " + shortlist.PlayerCount));
                card.Add(new Label("Status: " + shortlist.Status));
                card.Add(new Label("Updated: " + shortlist.UpdatedAt));
                var open = new Button { text = selectedShortlistId == shortlist.ShortlistId ? "Viewing" : "View" };
                open.clicked += () =>
                {
                    detail.Clear();
                    using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                    {
                        RenderDetail(databasePath, new ShortlistWorkflowService(factory).BuildDetailViewModel(shortlist.ShortlistId), detail, overview, message);
                    }
                };
                card.Add(open);
                grid.Add(card);
            }
        }

        private static void RenderDetail(string databasePath, ShortlistDetailViewModel detailModel, VisualElement detail, VisualElement overview, Label message)
        {
            detail.Clear();
            if (detailModel == null || detailModel.ShortlistId == 0)
            {
                detail.Add(StatlynUiFactory.MakeCard("Shortlist Detail", new[] { "Select or create a shortlist." }));
                return;
            }

            var panel = new VisualElement();
            panel.AddToClassList("visual-section");
            panel.Add(StatlynUiFactory.MakeSectionTitle(detailModel.Name));
            panel.Add(new Label(detailModel.Description));
            panel.Add(new Label(detailModel.IsArchived ? "Archived" : "Active"));
            detail.Add(panel);

            if (detailModel.Players.Count == 0)
            {
                detail.Add(StatlynUiFactory.MakeCard("No Players", new[] { "Add players from Recruitment Centre or Player Profile.", "No fake shortlist rows are shown." }));
                return;
            }

            var grid = new VisualElement();
            grid.AddToClassList("dashboard-grid");
            detail.Add(grid);
            foreach (var player in detailModel.Players)
            {
                grid.Add(MakePlayerCard(databasePath, player, overview, detail, message));
            }
        }

        private static VisualElement MakePlayerCard(string databasePath, ShortlistPlayerRowViewModel player, VisualElement overview, VisualElement detail, Label message)
        {
            var card = new VisualElement();
            card.AddToClassList("glass-card");
            card.Add(StatlynUiFactory.MakeSectionTitle(player.PlayerName));
            card.Add(new Label(player.Age + " | " + player.Nationality + " | " + player.Position));
            card.Add(new Label("Source: " + player.SourceName));
            card.Add(new Label("Role: " + player.RoleName));
            card.Add(new Label("Role fit: " + player.RoleFit + " | Confidence: " + player.Confidence));
            card.Add(new Label("Recommendation: " + player.Recommendation));
            card.Add(new Label("Status: " + player.Status + " | Priority: " + player.Priority));
            card.Add(new Label("Follow-up: " + player.FollowUpAction));
            card.Add(new Label("Output: " + (player.KeyOutputMetrics.Count == 0 ? "Output metrics missing" : string.Join(", ", player.KeyOutputMetrics))));
            card.Add(new Label("Missing data: " + player.MissingDataCount.ToString(CultureInfo.InvariantCulture) + " | Blocked fields: " + player.BlockedFieldCount.ToString(CultureInfo.InvariantCulture)));
            card.Add(new Label(player.IsLiveFm26Data ? "Live FM26 data" : "No live FM26 data"));
            card.Add(new Label("Scout report: " + LoadScoutReportLabel(databasePath, player.StatlynPlayerId)));

            var status = new DropdownField("Status", EnumNames<ShortlistStatus>(), SafeIndex(EnumNames<ShortlistStatus>(), player.Status));
            var priority = new DropdownField("Priority", EnumNames<ShortlistPriority>(), SafeIndex(EnumNames<ShortlistPriority>(), player.Priority));
            var followUp = new DropdownField("Follow-up", EnumNames<ShortlistFollowUpAction>(), SafeIndex(EnumNames<ShortlistFollowUpAction>(), player.FollowUpAction));
            var note = new TextField("User note");
            note.value = player.UserNote;
            card.Add(status);
            card.Add(priority);
            card.Add(followUp);
            card.Add(note);

            var actions = new VisualElement();
            actions.AddToClassList("action-row");
            card.Add(actions);
            var scout = new Button { text = "Create Scout Assignment" };
            var save = new Button { text = "Update" };
            var remove = new Button { text = "Remove" };
            actions.Add(scout);
            actions.Add(save);
            actions.Add(remove);

            scout.clicked += () =>
            {
                try
                {
                    using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                    {
                        var result = new ScoutDeskWorkflowService(factory).CreateAssignmentFromShortlistPlayer(player.ShortlistPlayerId, string.Empty, null);
                        message.text = result.SafeMessage;
                    }
                }
                catch (Exception ex)
                {
                    message.text = "Could not create scout assignment safely: " + ex.GetType().Name + ": " + ex.Message;
                }
            };

            save.clicked += () =>
            {
                try
                {
                    using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                    {
                        var service = new ShortlistWorkflowService(factory);
                        service.UpdatePlayer(new ShortlistUpdatePlayerRequest
                        {
                            ShortlistPlayerId = player.ShortlistPlayerId,
                            Status = ParseEnum(status.value, ShortlistStatus.Watchlist),
                            Priority = ParseEnum(priority.value, ShortlistPriority.Medium),
                            FollowUpAction = ParseEnum(followUp.value, ShortlistFollowUpAction.None),
                            RoleName = player.RoleName,
                            Recommendation = player.Recommendation,
                            UserNote = note.value
                        });
                        message.text = "Shortlist player updated safely.";
                        RenderShortlists(databasePath, overview, detail, message, 0);
                    }
                }
                catch (Exception ex)
                {
                    message.text = "Could not update shortlist player safely: " + ex.GetType().Name + ": " + ex.Message;
                }
            };

            remove.clicked += () =>
            {
                try
                {
                    using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                    {
                        new ShortlistWorkflowService(factory).RemovePlayer(player.ShortlistPlayerId);
                        message.text = "Player removed from shortlist.";
                        RenderShortlists(databasePath, overview, detail, message, 0);
                    }
                }
                catch (Exception ex)
                {
                    message.text = "Could not remove shortlist player safely: " + ex.GetType().Name + ": " + ex.Message;
                }
            };

            return card;
        }

        private static List<string> EnumNames<TEnum>()
        {
            return new List<string>(Enum.GetNames(typeof(TEnum)));
        }

        private static string LoadScoutReportLabel(string databasePath, string statlynPlayerId)
        {
            try
            {
                using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                {
                    var summary = new ScoutDeskWorkflowService(factory).BuildLatestReportSummary(statlynPlayerId);
                    if (!summary.HasReport)
                    {
                        return summary.Summary;
                    }

                    return summary.Recommendation + " | Confidence: " + summary.Confidence + " | " + summary.ReportDate + " | " + summary.Summary;
                }
            }
            catch
            {
                return "Scout report unavailable.";
            }
        }

        private static int SafeIndex(List<string> values, string value)
        {
            var index = values.IndexOf(value);
            return index < 0 ? 0 : index;
        }

        private static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct
        {
            return Enum.TryParse<TEnum>(value, out var parsed) ? parsed : fallback;
        }
    }
}
