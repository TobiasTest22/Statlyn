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
    public sealed class ScoutDeskPageBuilder
    {
        public void Build(VisualElement main)
        {
            main.Clear();
            var databasePath = new StatlynDatabasePathResolver().ResolvePath(Application.persistentDataPath, StatlynDatabasePathMode.RuntimeMain);
            BuildHeader(main);

            var form = new VisualElement();
            form.AddToClassList("data-source-form");
            main.Add(form);

            var statlynPlayerId = new TextField("StatlynPlayerId");
            form.Add(statlynPlayerId);
            var roleName = new TextField("Role assessed");
            form.Add(roleName);
            var assignedTo = new TextField("Assigned to");
            form.Add(assignedTo);
            var priority = new DropdownField("Priority", EnumNames<ShortlistPriority>(), SafeIndex(EnumNames<ShortlistPriority>(), ShortlistPriority.Medium.ToString()));
            form.Add(priority);

            var actions = new VisualElement();
            actions.AddToClassList("action-row");
            form.Add(actions);
            var create = new Button { text = "Create Assignment" };
            var createFirstShortlisted = new Button { text = "Create For First Shortlisted" };
            var refresh = new Button { text = "Refresh" };
            actions.Add(create);
            actions.Add(createFirstShortlisted);
            actions.Add(refresh);

            var message = new Label(string.Empty);
            message.AddToClassList("card-row");
            form.Add(message);

            var assignments = new VisualElement();
            assignments.AddToClassList("data-source-results");
            main.Add(assignments);
            var detail = new VisualElement();
            detail.AddToClassList("data-source-results");
            main.Add(detail);

            RenderScoutDesk(databasePath, assignments, detail, message, 0);

            create.clicked += () =>
            {
                try
                {
                    using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                    {
                        var result = new ScoutDeskWorkflowService(factory).CreateAssignment(new CreateScoutAssignmentRequest
                        {
                            StatlynPlayerId = statlynPlayerId.value,
                            RoleName = roleName.value,
                            Priority = ParseEnum(priority.value, ShortlistPriority.Medium),
                            AssignedTo = assignedTo.value
                        });
                        message.text = result.SafeMessage;
                        RenderScoutDesk(databasePath, assignments, detail, message, result.Assignment == null ? 0 : result.Assignment.Id);
                    }
                }
                catch (Exception ex)
                {
                    message.text = "Could not create scout assignment safely: " + ex.GetType().Name + ": " + ex.Message;
                }
            };

            createFirstShortlisted.clicked += () =>
            {
                try
                {
                    var shortlistPlayerId = FindFirstShortlistedPlayerId(databasePath);
                    if (shortlistPlayerId == 0)
                    {
                        message.text = "No shortlisted players are available yet.";
                        return;
                    }

                    using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                    {
                        var result = new ScoutDeskWorkflowService(factory).CreateAssignmentFromShortlistPlayer(shortlistPlayerId, assignedTo.value, null);
                        message.text = result.SafeMessage;
                        RenderScoutDesk(databasePath, assignments, detail, message, result.Assignment == null ? 0 : result.Assignment.Id);
                    }
                }
                catch (Exception ex)
                {
                    message.text = "Could not create scout assignment safely: " + ex.GetType().Name + ": " + ex.Message;
                }
            };

            refresh.clicked += () => RenderScoutDesk(databasePath, assignments, detail, message, 0);
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
            var title = new Label("Scout Desk");
            title.AddToClassList("screen-title");
            titleStack.Add(title);
            var subtitle = new Label("Human scouting workflow - qualitative observations only");
            subtitle.AddToClassList("screen-subtitle");
            titleStack.Add(subtitle);

            var status = new Label("Local reports");
            status.AddToClassList("status-pill");
            header.Add(status);
        }

        private static void RenderScoutDesk(string databasePath, VisualElement assignments, VisualElement detail, Label message, long selectedAssignmentId)
        {
            assignments.Clear();
            detail.Clear();
            try
            {
                using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                {
                    var service = new ScoutDeskWorkflowService(factory);
                    var page = service.BuildPageViewModel(new ScoutDeskQuery());
                    message.text = string.IsNullOrWhiteSpace(message.text) ? page.SafeMessage : message.text;
                    RenderAssignments(databasePath, page, assignments, detail, message);
                    var selectedId = selectedAssignmentId != 0
                        ? selectedAssignmentId
                        : page.Assignments.Count == 0 ? 0 : page.Assignments[0].AssignmentId;
                    if (selectedId == 0)
                    {
                        detail.Add(StatlynUiFactory.MakeCard("Scout Assignment", new[] { "Create an assignment from a shortlist or persisted player ID.", "No fake scout rows are shown." }));
                        return;
                    }

                    RenderDetail(databasePath, service.BuildAssignmentDetailViewModel(selectedId), assignments, detail, message);
                }
            }
            catch (Exception ex)
            {
                assignments.Add(StatlynUiFactory.MakeCard("Scout Desk", new[] { "Could not load Scout Desk safely.", ex.GetType().Name + ": " + ex.Message }));
            }
        }

        private static void RenderAssignments(string databasePath, ScoutDeskPageViewModel page, VisualElement assignments, VisualElement detail, Label message)
        {
            var grid = new VisualElement();
            grid.AddToClassList("dashboard-grid");
            assignments.Add(grid);

            if (page.Assignments.Count == 0)
            {
                grid.Add(StatlynUiFactory.MakeCard("No Assignments", new[] { "Create a scout assignment from Shortlists or a persisted player ID.", "No live FM26 data." }));
                return;
            }

            foreach (var assignment in page.Assignments)
            {
                var card = new VisualElement();
                card.AddToClassList("glass-card");
                card.Add(StatlynUiFactory.MakeSectionTitle(assignment.PlayerName));
                card.Add(new Label(assignment.Position + " | " + assignment.Source));
                card.Add(new Label("Role: " + assignment.Role));
                card.Add(new Label("Status: " + assignment.AssignmentStatus + " | Priority: " + assignment.Priority));
                card.Add(new Label("Assigned to: " + (string.IsNullOrWhiteSpace(assignment.AssignedTo) ? "Unassigned" : assignment.AssignedTo)));
                card.Add(new Label("Due: " + assignment.DueDate));
                card.Add(new Label("Latest report: " + assignment.LatestReportRecommendation + " | Confidence: " + assignment.ScoutConfidence));
                card.Add(new Label("Missing output: " + assignment.MissingOutputCount + " | Blocked audits: " + assignment.BlockedAuditCount));
                card.Add(new Label(assignment.NoLiveFm26Label));

                var open = new Button { text = "View Assignment" };
                open.clicked += () =>
                {
                    using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                    {
                        RenderDetail(databasePath, new ScoutDeskWorkflowService(factory).BuildAssignmentDetailViewModel(assignment.AssignmentId), assignments, detail, message);
                    }
                };
                card.Add(open);
                grid.Add(card);
            }
        }

        private static void RenderDetail(string databasePath, ScoutAssignmentDetailViewModel detailModel, VisualElement assignments, VisualElement detail, Label message)
        {
            detail.Clear();
            if (detailModel == null || detailModel.Assignment == null || detailModel.Assignment.AssignmentId == 0)
            {
                detail.Add(StatlynUiFactory.MakeCard("Scout Assignment", new[] { "Select or create a scout assignment." }));
                return;
            }

            var assignment = detailModel.Assignment;
            var panel = new VisualElement();
            panel.AddToClassList("visual-section");
            panel.Add(StatlynUiFactory.MakeSectionTitle(assignment.PlayerName));
            panel.Add(new Label("Role: " + assignment.Role + " | Status: " + assignment.AssignmentStatus));
            panel.Add(new Label(assignment.NoLiveFm26Label));
            panel.Add(new Label(detailModel.SafeNotice));
            detail.Add(panel);

            var promptPanel = new VisualElement();
            promptPanel.AddToClassList("visual-panel");
            promptPanel.Add(StatlynUiFactory.MakeSectionTitle("Scout Questions"));
            detail.Add(promptPanel);
            foreach (var prompt in detailModel.Questions)
            {
                promptPanel.Add(new Label(prompt.Category + ": " + prompt.Question));
                promptPanel.Add(new Label(prompt.WhyItMatters));
            }

            detail.Add(MakeReportForm(databasePath, assignment, detailModel.Questions, assignments, detail, message));
            detail.Add(MakeReportHistory(detailModel.ReportHistory));
        }

        private static VisualElement MakeReportForm(
            string databasePath,
            ScoutAssignmentCardViewModel assignment,
            IReadOnlyList<ScoutQuestionPromptViewModel> prompts,
            VisualElement assignments,
            VisualElement detail,
            Label message)
        {
            var form = new VisualElement();
            form.AddToClassList("data-source-form");
            form.Add(StatlynUiFactory.MakeSectionTitle("Scout Report"));

            var role = new TextField("Role assessed");
            role.value = assignment.Role;
            form.Add(role);

            var ratingNames = EnumNames<ScoutObservationRating>();
            var technical = new DropdownField("Technical", ratingNames, SafeIndex(ratingNames, ScoutObservationRating.Unknown.ToString()));
            var tactical = new DropdownField("Tactical", ratingNames, SafeIndex(ratingNames, ScoutObservationRating.Unknown.ToString()));
            var physical = new DropdownField("Physical", ratingNames, SafeIndex(ratingNames, ScoutObservationRating.Unknown.ToString()));
            var mental = new DropdownField("Mental/character", ratingNames, SafeIndex(ratingNames, ScoutObservationRating.Unknown.ToString()));
            form.Add(technical);
            form.Add(tactical);
            form.Add(physical);
            form.Add(mental);

            var recommendationNames = EnumNames<ScoutReportRecommendation>();
            var recommendation = new DropdownField("Recommendation", recommendationNames, SafeIndex(recommendationNames, ScoutReportRecommendation.ScoutFurther.ToString()));
            form.Add(recommendation);
            var followUpNames = EnumNames<ScoutFollowUpAction>();
            var followUp = new DropdownField("Follow-up", followUpNames, SafeIndex(followUpNames, ScoutFollowUpAction.None.ToString()));
            form.Add(followUp);
            var confidence = new TextField("Confidence 0-100");
            confidence.value = "50";
            form.Add(confidence);

            var strengths = MakeMultiline("Strengths");
            var weaknesses = MakeMultiline("Weaknesses");
            var risks = MakeMultiline("Risks");
            var summary = MakeMultiline("Final summary");
            form.Add(strengths);
            form.Add(weaknesses);
            form.Add(risks);
            form.Add(summary);

            var answers = new List<Tuple<ScoutQuestionPromptViewModel, TextField>>();
            foreach (var prompt in prompts)
            {
                var answer = MakeMultiline(prompt.Question);
                answers.Add(Tuple.Create(prompt, answer));
                form.Add(answer);
            }

            var updateShortlist = new Toggle("Update linked shortlist from recommendation");
            form.Add(updateShortlist);

            var submit = new Button { text = "Submit Report" };
            submit.clicked += () =>
            {
                try
                {
                    using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                    {
                        var request = new SubmitScoutReportRequest
                        {
                            AssignmentId = assignment.AssignmentId,
                            StatlynPlayerId = assignment.StatlynPlayerId,
                            RoleAssessed = role.value,
                            TechnicalRating = ParseEnum(technical.value, ScoutObservationRating.Unknown),
                            TacticalRating = ParseEnum(tactical.value, ScoutObservationRating.Unknown),
                            PhysicalRating = ParseEnum(physical.value, ScoutObservationRating.Unknown),
                            MentalRating = ParseEnum(mental.value, ScoutObservationRating.Unknown),
                            OverallRecommendation = ParseEnum(recommendation.value, ScoutReportRecommendation.ScoutFurther),
                            Confidence = ParseInt(confidence.value, 50),
                            Strengths = strengths.value,
                            Weaknesses = weaknesses.value,
                            Risks = risks.value,
                            FollowUpAction = ParseEnum(followUp.value, ScoutFollowUpAction.None),
                            FinalSummary = summary.value,
                            UpdateShortlistFromReport = updateShortlist.value,
                            QuestionAnswers = BuildAnswers(answers)
                        };
                        var result = new ScoutDeskWorkflowService(factory).SubmitReport(request);
                        message.text = result.SafeMessage;
                        RenderScoutDesk(databasePath, assignments, detail, message, assignment.AssignmentId);
                    }
                }
                catch (Exception ex)
                {
                    message.text = "Could not submit scout report safely: " + ex.GetType().Name + ": " + ex.Message;
                }
            };
            form.Add(submit);
            return form;
        }

        private static VisualElement MakeReportHistory(ScoutReportHistoryViewModel history)
        {
            var panel = new VisualElement();
            panel.AddToClassList("visual-panel");
            panel.Add(StatlynUiFactory.MakeSectionTitle("Report History"));
            if (history == null || history.Reports.Count == 0)
            {
                panel.Add(new Label("No scout report yet."));
                return panel;
            }

            foreach (var report in history.Reports)
            {
                panel.Add(new Label(report.ReportDate + " | " + report.Recommendation + " | Confidence: " + report.Confidence));
                panel.Add(new Label(report.FinalSummary));
                panel.Add(new Label(report.SafeNotice));
            }

            return panel;
        }

        private static IReadOnlyList<ScoutQuestionAnswerRequest> BuildAnswers(IReadOnlyList<Tuple<ScoutQuestionPromptViewModel, TextField>> answers)
        {
            var output = new List<ScoutQuestionAnswerRequest>();
            foreach (var answer in answers)
            {
                output.Add(new ScoutQuestionAnswerRequest
                {
                    Category = answer.Item1.Category,
                    Question = answer.Item1.Question,
                    Answer = answer.Item2.value
                });
            }

            return output;
        }

        private static TextField MakeMultiline(string label)
        {
            var field = new TextField(label);
            field.multiline = true;
            return field;
        }

        private static long FindFirstShortlistedPlayerId(string databasePath)
        {
            try
            {
                using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                {
                    var page = new ShortlistWorkflowService(factory).BuildPageViewModel(includeArchived: false);
                    if (page.SelectedShortlist == null || page.SelectedShortlist.Players.Count == 0)
                    {
                        return 0;
                    }

                    return page.SelectedShortlist.Players[0].ShortlistPlayerId;
                }
            }
            catch
            {
                return 0;
            }
        }

        private static List<string> EnumNames<TEnum>()
        {
            return new List<string>(Enum.GetNames(typeof(TEnum)));
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

        private static int ParseInt(string value, int fallback)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
        }
    }
}
