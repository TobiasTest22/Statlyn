using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Statlyn.Data;
using Statlyn.Data.RoleLab;
using Statlyn.UI;
using Statlyn.UnityApp.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace Statlyn.UnityApp.Pages
{
    public sealed class RoleLabPageBuilder
    {
        public void Build(VisualElement main)
        {
            main.Clear();
            var databasePath = new StatlynDatabasePathResolver().ResolvePath(Application.persistentDataPath, StatlynDatabasePathMode.RuntimeMain);
            BuildHeader(main);
            main.Add(StatlynUiFactory.MakeCommandWarningBanner("Role Lab Guardrail", new[]
            {
                "Generic/import role templates only.",
                "Not official FM26 mappings.",
                "No old duty logic or hidden values."
            }));

            var message = new Label(string.Empty);
            message.AddToClassList("card-row");

            var actions = new VisualElement();
            var seed = new Button { text = "Seed Roles" };
            var refresh = new Button { text = "Refresh" };
            actions = StatlynUiFactory.MakeCommandActionButtonRow(seed, refresh);
            main.Add(actions);
            main.Add(message);

            var roleForm = MakeRoleForm(databasePath, message);
            main.Add(roleForm);

            var pairForm = new VisualElement();
            pairForm.AddToClassList("data-source-form");
            main.Add(pairForm);

            var roleList = new VisualElement();
            roleList.AddToClassList("data-source-results");
            main.Add(roleList);
            var detail = new VisualElement();
            detail.AddToClassList("data-source-results");
            main.Add(detail);

            RenderRoleLab(databasePath, roleList, detail, pairForm, message, 0);

            seed.clicked += () =>
            {
                try
                {
                    using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                    {
                        var result = new RoleLabWorkflowService(factory).SeedBuiltInRoles();
                        message.text = result.SafeMessage + " Roles: " + result.TotalRoles.ToString(CultureInfo.InvariantCulture);
                    }

                    RenderRoleLab(databasePath, roleList, detail, pairForm, message, 0);
                }
                catch (Exception ex)
                {
                    message.text = "Could not seed Role Lab safely: " + ex.GetType().Name + ": " + ex.Message;
                }
            };

            refresh.clicked += () => RenderRoleLab(databasePath, roleList, detail, pairForm, message, 0);
        }

        private static void BuildHeader(VisualElement main)
        {
            main.Add(StatlynUiFactory.MakeCommandPageHeader(
                "Role Lab",
                "Phase-aware generic/import role templates; not official FM26 mappings",
                "Generic/import metric",
                CommandStatusCategory.Info));
        }

        private static VisualElement MakeRoleForm(string databasePath, Label message)
        {
            var form = new VisualElement();
            form.AddToClassList("data-source-form");
            form.Add(StatlynUiFactory.MakeSectionTitle("Create Role"));

            var roleName = new TextField("Role name");
            form.Add(roleName);
            var phaseNames = EnumNames<TacticalPhase>();
            var phase = new DropdownField("Phase", phaseNames, SafeIndex(phaseNames, TacticalPhase.InPossession.ToString()));
            form.Add(phase);
            var familyNames = EnumNames<TacticalRoleFamily>();
            var family = new DropdownField("Role family", familyNames, SafeIndex(familyNames, TacticalRoleFamily.BuildUp.ToString()));
            form.Add(family);
            var positionGroup = new TextField("Position group");
            positionGroup.value = "CentralMidfield";
            form.Add(positionGroup);
            var movement = new TextField("Movement behaviour") { multiline = true };
            var buildUp = new TextField("Build-up behaviour") { multiline = true };
            var finalThird = new TextField("Final-third behaviour") { multiline = true };
            var pressing = new TextField("Pressing behaviour") { multiline = true };
            var defensive = new TextField("Defensive-block behaviour") { multiline = true };
            var transition = new TextField("Transition behaviour") { multiline = true };
            form.Add(movement);
            form.Add(buildUp);
            form.Add(finalThird);
            form.Add(pressing);
            form.Add(defensive);
            form.Add(transition);
            form.Add(new Label("Source: UserCreated | Generic/import role template; FM26 validation pending."));

            var save = new Button { text = "Save Role" };
            save.clicked += () =>
            {
                try
                {
                    using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                    {
                        var result = new RoleLabWorkflowService(factory).CreateUserRole(new CreateTacticalRoleRequest
                        {
                            RoleName = roleName.value,
                            TacticalPhase = ParseEnum(phase.value, TacticalPhase.InPossession),
                            RoleFamily = ParseEnum(family.value, TacticalRoleFamily.BuildUp),
                            PositionGroup = positionGroup.value,
                            ValidSlots = new[] { TacticalSlot.CMC },
                            MovementBehaviour = movement.value,
                            BuildUpBehaviour = buildUp.value,
                            FinalThirdBehaviour = finalThird.value,
                            PressingBehaviour = pressing.value,
                            DefensiveBlockBehaviour = defensive.value,
                            TransitionBehaviour = transition.value
                        });
                        message.text = result.SafeMessage;
                    }
                }
                catch (Exception ex)
                {
                    message.text = "Could not save Role Lab role safely: " + ex.GetType().Name + ": " + ex.Message;
                }
            };
            form.Add(save);
            return form;
        }

        private static void RenderRoleLab(string databasePath, VisualElement roleList, VisualElement detail, VisualElement pairForm, Label message, long selectedRoleId)
        {
            roleList.Clear();
            detail.Clear();
            pairForm.Clear();

            try
            {
                using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                {
                    var service = new RoleLabWorkflowService(factory);
                    var page = service.BuildPageViewModel(includeArchived: false);
                    message.text = string.IsNullOrWhiteSpace(message.text) ? page.SafeMessage : message.text;
                    RenderPairForm(databasePath, page, pairForm, message);
                    RenderRoleList(databasePath, page, roleList, detail, message);

                    var selectedId = selectedRoleId == 0 && page.Roles.Count > 0 ? page.Roles[0].RoleId : selectedRoleId;
                    var selected = selectedId == 0 ? page.SelectedRole : service.BuildRoleDetailViewModel(selectedId);
                    RenderDetail(selected, detail);
                }
            }
            catch (Exception ex)
            {
                roleList.Add(StatlynUiFactory.MakeErrorCard("Role Lab", "Could not load Role Lab safely.", ex.GetType().Name + ": " + ex.Message));
            }
        }

        private static void RenderPairForm(string databasePath, RoleLabPageViewModel page, VisualElement pairForm, Label message)
        {
            pairForm.Add(StatlynUiFactory.MakeSectionTitle("Create Role Pair"));
            if (page.Roles.Count < 2)
            {
                pairForm.Add(new Label("Seed or create at least two roles to create a role pair."));
                return;
            }

            var roleNames = page.Roles.Select(role => role.RoleName).ToList();
            var roleIds = page.Roles.ToDictionary(role => role.RoleName, role => role.RoleId);
            var pairName = new TextField("Pair name");
            pairForm.Add(pairName);
            var ipRole = new DropdownField("IP role", roleNames, 0);
            var oopRole = new DropdownField("OOP role", roleNames, Math.Min(1, roleNames.Count - 1));
            pairForm.Add(ipRole);
            pairForm.Add(oopRole);
            var slots = EnumNames<TacticalSlot>();
            var ipSlot = new DropdownField("IP slot", slots, SafeIndex(slots, TacticalSlot.CMC.ToString()));
            var oopSlot = new DropdownField("OOP slot", slots, SafeIndex(slots, TacticalSlot.CMC.ToString()));
            pairForm.Add(ipSlot);
            pairForm.Add(oopSlot);
            var ipFormation = new TextField("IP formation");
            var oopFormation = new TextField("OOP formation");
            var complexity = new TextField("Transition complexity 0-100");
            var risk = new TextField("Tactical risk 0-100");
            var familiarity = new TextField("Positional familiarity need");
            complexity.value = "50";
            risk.value = "50";
            pairForm.Add(ipFormation);
            pairForm.Add(oopFormation);
            pairForm.Add(complexity);
            pairForm.Add(risk);
            pairForm.Add(familiarity);

            var save = new Button { text = "Save Pair" };
            save.clicked += () =>
            {
                try
                {
                    using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                    {
                        var result = new RoleLabWorkflowService(factory).CreateRolePair(new CreateTacticalRolePairRequest
                        {
                            PairName = pairName.value,
                            InPossessionRoleId = roleIds[ipRole.value],
                            OutOfPossessionRoleId = roleIds[oopRole.value],
                            InPossessionSlot = ParseEnum(ipSlot.value, TacticalSlot.CMC),
                            OutOfPossessionSlot = ParseEnum(oopSlot.value, TacticalSlot.CMC),
                            InPossessionFormation = ipFormation.value,
                            OutOfPossessionFormation = oopFormation.value,
                            TransitionComplexityScore = ParseInt(complexity.value, 50),
                            TacticalRiskScore = ParseInt(risk.value, 50),
                            PositionalFamiliarityNeed = familiarity.value
                        });
                        message.text = result.SafeMessage;
                    }
                }
                catch (Exception ex)
                {
                    message.text = "Could not save Role Lab pair safely: " + ex.GetType().Name + ": " + ex.Message;
                }
            };
            pairForm.Add(save);
        }

        private static void RenderRoleList(string databasePath, RoleLabPageViewModel page, VisualElement roleList, VisualElement detail, Label message)
        {
            var grid = new VisualElement();
            grid.AddToClassList("dashboard-grid");
            grid.AddToClassList("command-kpi-row");
            roleList.Add(grid);

            if (page.Roles.Count == 0)
            {
                grid.Add(StatlynUiFactory.MakeCommandEmptyState("No Roles", "Seed generic/import templates or create a user role.", "No official FM26 mapping is claimed."));
                return;
            }

            foreach (var role in page.Roles)
            {
                var card = new VisualElement();
                card.AddToClassList("glass-card");
                card.AddToClassList("command-panel");
                card.Add(StatlynUiFactory.MakeSectionTitle(role.RoleName));
                card.Add(new Label(role.Phase + " | " + role.Family));
                card.Add(new Label("Source: " + role.Source));
                card.Add(new Label(role.OfficialStatus));
                card.Add(new Label("Metrics: " + role.MetricRequirementCount + " | Questions: " + role.ScoutQuestionCount + " | Red flags: " + role.RedFlagCount));
                card.Add(new Label("Slots: " + role.ValidSlots));
                var open = new Button { text = "View Role" };
                open.clicked += () =>
                {
                    using (var factory = RuntimeDatabaseFactory.CreateFile(databasePath))
                    {
                        RenderDetail(new RoleLabWorkflowService(factory).BuildRoleDetailViewModel(role.RoleId), detail);
                        message.text = "Role Lab detail loaded safely.";
                    }
                };
                card.Add(open);
                grid.Add(card);
            }

            if (page.RolePairs.Count > 0)
            {
                foreach (var pair in page.RolePairs)
                {
                    grid.Add(StatlynUiFactory.MakeCard(pair.PairName, new[]
                    {
                        "IP: " + pair.InPossessionRole + " (" + pair.InPossessionSlot + ")",
                        "OOP: " + pair.OutOfPossessionRole + " (" + pair.OutOfPossessionSlot + ")",
                        "Transition complexity: " + pair.TransitionComplexityScore + " | Tactical risk: " + pair.TacticalRiskScore
                    }));
                }
            }
        }

        private static void RenderDetail(TacticalRoleDetailViewModel detailModel, VisualElement detail)
        {
            detail.Clear();
            if (detailModel == null)
            {
                detail.Add(StatlynUiFactory.MakeCommandEmptyState("Role Detail", "Select or create a role."));
                return;
            }

            var panel = new VisualElement();
            panel.AddToClassList("visual-section");
            panel.Add(StatlynUiFactory.MakeSectionTitle(detailModel.Role.RoleName));
            panel.Add(new Label(detailModel.Role.Phase + " | " + detailModel.Role.Family));
            panel.Add(new Label(detailModel.Role.OfficialStatus));
            panel.Add(new Label(detailModel.SafeNotice));
            panel.Add(new Label("Movement: " + detailModel.MovementBehaviour));
            panel.Add(new Label("Build-up: " + detailModel.BuildUpBehaviour));
            panel.Add(new Label("Final third: " + detailModel.FinalThirdBehaviour));
            panel.Add(new Label("Pressing: " + detailModel.PressingBehaviour));
            panel.Add(new Label("Defensive block: " + detailModel.DefensiveBlockBehaviour));
            panel.Add(new Label("Transition: " + detailModel.TransitionBehaviour));
            detail.Add(panel);

            detail.Add(StatlynUiFactory.MakeCommandDataQualityPanel("Metric Requirements", detailModel.MetricRequirements.Count == 0
                ? new[] { "No metric requirements yet." }
                : detailModel.MetricRequirements.Select(item => item.FieldName + " | " + item.Importance + " | " + item.Direction), detailModel.MetricRequirements.Count == 0 ? CommandStatusCategory.Muted : CommandStatusCategory.Info));
            detail.Add(StatlynUiFactory.MakeCommandPanel("Scout Questions", detailModel.ScoutQuestions.Count == 0
                ? new[] { "No scout questions yet." }
                : detailModel.ScoutQuestions.Select(item => item.Category + ": " + item.Question)));
            detail.Add(StatlynUiFactory.MakeCommandWarningBanner("Red Flags", detailModel.RedFlags.Count == 0
                ? new[] { "No red flags yet." }
                : detailModel.RedFlags.Select(item => item.FieldName + " " + item.Operator + " " + item.Threshold + " | " + item.Message)));
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
