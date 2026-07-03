using System;
using System.Collections.Generic;
using System.Linq;

namespace Statlyn.Data.RoleLab
{
    public sealed class RoleLabWorkflowService
    {
        private readonly StatlynDbConnectionFactory _connectionFactory;
        private readonly RoleLabRepository _repository;
        private readonly RoleLabSeedService _seedService;

        public RoleLabWorkflowService(StatlynDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _repository = new RoleLabRepository(connectionFactory);
            _seedService = new RoleLabSeedService(connectionFactory);
        }

        public RoleLabSeedResult SeedBuiltInRoles()
        {
            return _seedService.SeedBuiltInRoles();
        }

        public RoleLabPageViewModel BuildPageViewModel(bool includeArchived)
        {
            var roles = _repository.LoadRoles(includeArchived);
            var roleCards = roles.Select(BuildRoleCard).ToList();
            var pairCards = _repository.LoadRolePairs(includeArchived)
                .Select(pair => new TacticalRolePairCardViewModel(pair, _repository.LoadRole(pair.InPossessionRoleId), _repository.LoadRole(pair.OutOfPossessionRoleId)))
                .ToList();
            var selected = roles.Count == 0 ? null : BuildRoleDetailViewModel(roles[0].Id);
            return new RoleLabPageViewModel(
                roleCards,
                pairCards,
                selected,
                roles.Count == 0 ? "No Role Lab templates yet. Seed built-in generic/import templates to start." : "Role Lab templates loaded from local SQLite.",
                _connectionFactory.DatabasePath);
        }

        public TacticalRoleDetailViewModel? BuildRoleDetailViewModel(long roleId)
        {
            var role = _repository.LoadRole(roleId);
            if (role == null)
            {
                return null;
            }

            return new TacticalRoleDetailViewModel(
                BuildRoleCard(role),
                role.MovementBehaviour,
                role.BuildUpBehaviour,
                role.FinalThirdBehaviour,
                role.PressingBehaviour,
                role.DefensiveBlockBehaviour,
                role.TransitionBehaviour,
                _repository.LoadMetricRequirementsForRole(role.Id).Select(item => new MetricRequirementViewModel(item)).ToList(),
                _repository.LoadScoutQuestionsForRole(role.Id).Select(item => new ScoutQuestionViewModel(item)).ToList(),
                _repository.LoadRedFlagsForRole(role.Id).Select(item => new RedFlagViewModel(item)).ToList());
        }

        public RoleLabWorkflowResult CreateUserRole(CreateTacticalRoleRequest request)
        {
            try
            {
                request = request ?? new CreateTacticalRoleRequest();
                var now = DateTimeOffset.UtcNow;
                var role = _repository.SaveRole(new TacticalRoleModel(
                    0,
                    request.RoleName,
                    request.TacticalPhase,
                    request.RoleFamily,
                    TacticalRoleSource.UserCreated,
                    false,
                    string.Empty,
                    request.PositionGroup,
                    request.ValidSlots == null || request.ValidSlots.Count == 0 ? new[] { TacticalSlot.CMC } : request.ValidSlots,
                    request.MovementBehaviour,
                    request.BuildUpBehaviour,
                    request.FinalThirdBehaviour,
                    request.PressingBehaviour,
                    request.DefensiveBlockBehaviour,
                    request.TransitionBehaviour,
                    now,
                    now,
                    false));
                return new RoleLabWorkflowResult(true, "Role Lab user role saved as a generic/import template.", role, null, SafetyWarnings(), new List<string>());
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                return RoleLabWorkflowResult.Failure("Role Lab role could not be saved safely.", new[] { ex.Message });
            }
        }

        public RoleLabWorkflowResult EditUserRole(TacticalRoleModel role)
        {
            try
            {
                if (role == null)
                {
                    throw new ArgumentNullException(nameof(role));
                }

                var saved = _repository.SaveRole(new TacticalRoleModel(
                    role.Id,
                    role.RoleName,
                    role.TacticalPhase,
                    role.RoleFamily,
                    role.Source == TacticalRoleSource.BuiltInSeed ? TacticalRoleSource.UserCreated : role.Source,
                    false,
                    string.Empty,
                    role.PositionGroup,
                    role.ValidSlots,
                    role.MovementBehaviour,
                    role.BuildUpBehaviour,
                    role.FinalThirdBehaviour,
                    role.PressingBehaviour,
                    role.DefensiveBlockBehaviour,
                    role.TransitionBehaviour,
                    role.CreatedAtUtc,
                    DateTimeOffset.UtcNow,
                    role.IsArchived));
                return new RoleLabWorkflowResult(true, "Role Lab role updated safely.", saved, null, SafetyWarnings(), new List<string>());
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                return RoleLabWorkflowResult.Failure("Role Lab role could not be updated safely.", new[] { ex.Message });
            }
        }

        public void ArchiveRole(long roleId)
        {
            _repository.ArchiveRole(roleId);
        }

        public RoleLabWorkflowResult CreateRolePair(CreateTacticalRolePairRequest request)
        {
            try
            {
                request = request ?? new CreateTacticalRolePairRequest();
                var now = DateTimeOffset.UtcNow;
                var pair = _repository.SaveRolePair(new TacticalRolePairModel(
                    0,
                    request.PairName,
                    request.InPossessionRoleId,
                    request.OutOfPossessionRoleId,
                    request.InPossessionSlot,
                    request.OutOfPossessionSlot,
                    request.InPossessionFormation,
                    request.OutOfPossessionFormation,
                    request.TransitionComplexityScore,
                    request.TacticalRiskScore,
                    request.PositionalFamiliarityNeed,
                    now,
                    now,
                    false));
                return new RoleLabWorkflowResult(true, "Role Lab role pair saved. FM26 validation is still pending.", null, pair, SafetyWarnings(), new List<string>());
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                return RoleLabWorkflowResult.Failure("Role Lab role pair could not be saved safely.", new[] { ex.Message });
            }
        }

        public RoleLabWorkflowResult EditRolePair(TacticalRolePairModel pair)
        {
            try
            {
                var saved = _repository.SaveRolePair(pair);
                return new RoleLabWorkflowResult(true, "Role Lab role pair updated safely.", null, saved, SafetyWarnings(), new List<string>());
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
            {
                return RoleLabWorkflowResult.Failure("Role Lab role pair could not be updated safely.", new[] { ex.Message });
            }
        }

        public void ArchiveRolePair(long pairId)
        {
            _repository.ArchiveRolePair(pairId);
        }

        public RoleOutputMetricRequirementModel AddMetricRequirement(RoleOutputMetricRequirementModel requirement)
        {
            return _repository.SaveMetricRequirement(requirement);
        }

        public void RemoveMetricRequirement(long requirementId)
        {
            _repository.DeleteMetricRequirement(requirementId);
        }

        public RoleScoutQuestionModel AddScoutQuestion(RoleScoutQuestionModel question)
        {
            return _repository.SaveScoutQuestion(question);
        }

        public void RemoveScoutQuestion(long questionId)
        {
            _repository.DeleteScoutQuestion(questionId);
        }

        public RoleRedFlagModel AddRedFlag(RoleRedFlagModel redFlag)
        {
            return _repository.SaveRedFlag(redFlag);
        }

        public void RemoveRedFlag(long redFlagId)
        {
            _repository.DeleteRedFlag(redFlagId);
        }

        private TacticalRoleCardViewModel BuildRoleCard(TacticalRoleModel role)
        {
            return new TacticalRoleCardViewModel(
                role,
                _repository.LoadMetricRequirementsForRole(role.Id).Count,
                _repository.LoadScoutQuestionsForRole(role.Id).Count,
                _repository.LoadRedFlagsForRole(role.Id).Count);
        }

        private static IReadOnlyList<string> SafetyWarnings()
        {
            return new[] { "Generic/import role template; FM26 validation pending.", "Output metrics lead and attributes remain support-only." };
        }
    }
}
