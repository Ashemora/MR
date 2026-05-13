using DG.Tweening;
using Project.Scripts.Configs.Battle.Units;
using Project.Scripts.Configs.Battle.Visuals;
using Project.Scripts.Gameplay.Battle.Targeting;
using Project.Scripts.Gameplay.UI;
using R3;
using TMPro;
using UnityEngine;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Gameplay.Battle.Units
{
    public class HeroSlotView : MonoBehaviour, ITargetable
    {
        private const float DisabledPortraitBrightness = 0.45f;
        private const float AbilityPowerAnimDuration = 0.25f;
        private static readonly int FillEnabledShaderId = Shader.PropertyToID("_FillEnabled");
        private static readonly int FillReplaceShaderId = Shader.PropertyToID("_FillReplace");
        private static readonly int GrayscaleEnabledShaderId = Shader.PropertyToID("_GrayscaleEnabled");

        
        [Tooltip("SpriteRenderer, определяющий границы слота для таргетинга; не используется для окраски")]
        [SerializeField] private SpriteRenderer _boundsSource;

        [Space(10)]
        [Tooltip("SpriteRenderers, которые красятся цветом элемента героя")]
        [SerializeField] private SpriteRenderer[] _energyColoredRenderers;

        [Space(10)]
        [Tooltip("SpriteRenderers, которые красятся в DeathColor из UnitDeathConfig при гибели героя")]
        [SerializeField] private SpriteRenderer[] _deathColoredRenderers;

        [Space(10)]
        [Tooltip("Portrait SpriteRenderer - displays the character sprite and flashes on hit")]
        [SerializeField] private SpriteRenderer _portrait;

        [Tooltip("SpriteRenderer свечения с материалом Additive - отображается как подсветка источника или цели")]
        [SerializeField] private SpriteRenderer _glow;
        
        [Tooltip("Radial cooldown overlay - shown on top of portrait during cooldown")]
        [SerializeField] private CooldownSweepView _cooldownSweep;

        [Tooltip("Основная полоса HP - мгновенно обновляется при получении урона")]
        [SerializeField] private BarRenderer _hpBar;

        [Tooltip("Лаг-полоса HP позади основной полосы - опустошается с задержкой после получения урона")]
        [SerializeField] private BarRenderer _hpLagBar;

        [Tooltip("Текст HP (только текущее значение) - скрывается при MaxHP = 0 (бессмертный юнит)")]
        [SerializeField] private TMP_Text _hpText;

        [Tooltip("Корневой объект визуала щита. Если назначен, скрывается целиком пока щита нет")]
        [SerializeField] private GameObject _shieldRoot;

        [Tooltip("Основная полоса щита - отображает суммарную текущую прочность всех shield-слоев")]
        [SerializeField] private BarRenderer _shieldBar;

        [Tooltip("Лаг-полоса щита позади основной полосы")]
        [SerializeField] private BarRenderer _shieldLagBar;

        [Tooltip("Текст щита (только текущее значение) - скрывается, когда щита нет")]
        [SerializeField] private TMP_Text _shieldText;

        [Tooltip("Текст стоимости активации способности в единицах энергии")]
        [SerializeField] private TMP_Text _energyCostLabel;

        [Tooltip("Текст величины силы способности (урон / лечение)")]
        [SerializeField] private TMP_Text _abilityPowerLabel;

        [Tooltip("Якорь для всплывающих чисел урона/лечения - по умолчанию центр слота, если не назначен")]
        [SerializeField] private Transform _hitAnchor;

        [Space(10)]
        [Tooltip("GameObjects, которые активируются при смерти героя")]
        [SerializeField] private GameObject[] _activateOnDeath;

        [Space(10)]
        [Tooltip("GameObjects, которые деактивируются при смерти героя")]
        [SerializeField] private GameObject[] _deactivateOnDeath;

        [Space(10)]
        [Tooltip("GameObjects, которые включаются, пока у героя активна пассивная способность, связанная с типом его ячейки")]
        [SerializeField] private GameObject[] _activateOnSlotKindPassive;

        [Space(10)]
        [Tooltip("GameObjects, которые выключаются, пока у героя активна пассивная способность, связанная с типом его ячейки")]
        [SerializeField] private GameObject[] _deactivateOnSlotKindPassive;

        
        public Transform HitAnchor => _hitAnchor ? _hitAnchor : transform;
        public UnitDescriptor Descriptor => UnitDescriptor.Hero(_viewModel.Side, _viewModel.SlotIndex);
        public UnitActionType ActionType => _viewModel.ActionType;
        public bool IsReadySource => _viewModel is { IsAssigned: true } && _viewModel.IsActivatable.CurrentValue;
        public Bounds WorldBounds => _boundsSource ? _boundsSource.bounds : new Bounds(transform.position, Vector3.one);

        
        private HeroSlotViewModel _viewModel;
        private BattleAnimationConfig _config;
        private UnitDeathConfig _deathConfig;
        private CompositeDisposable _disposables;
        private Color _originalPortraitColor;
        private (float Remaining, float Duration) _cooldownProgress;
        private (float Remaining, float Duration) _stunProgress;
        private Vector3 _originalLocalPos;
        private Color[] _originalDeathColors;
        private Tween _hitFlashTween;
        private Tween _knockbackTween;
        private AnimatedIntegerText _abilityPowerTextTween;
        private MaterialPropertyBlock _portraitPropertyBlock;
        private bool _isAvailabilityDimmed;

        
        private void OnDestroy()
        {
            _hitFlashTween?.Kill();
            _knockbackTween?.Kill();
            _abilityPowerTextTween?.Dispose();
            _disposables?.Dispose();
        }
        

        public void Bind(HeroSlotViewModel viewModel, BattleAnimationConfig config, UnitDeathConfig deathConfig)
        {
            _viewModel = viewModel;
            _config = config;
            _deathConfig = deathConfig;

            _disposables?.Dispose();
            _disposables = new CompositeDisposable();
            _cooldownProgress = default;
            _stunProgress = default;
            _originalLocalPos = transform.localPosition;
            ResetPortraitDeathFill();
            CacheDeathColors();

            BindPortrait(viewModel);
            BindHPBars(viewModel);
            BindShieldBars(viewModel);
            BindHitReaction(viewModel);
            BindDeathState(viewModel);
            BindAvailabilityState(viewModel);
            BindCooldownSweep(viewModel);
            BindSlotKindPassiveState(viewModel);
            BindEnergyCostLabel(viewModel);
            BindAbilityPowerLabel(viewModel);
        }

        public bool IsValidTarget(UnitDescriptor source, UnitActionType sourceActionType)
        {
            if (null == _viewModel || false == _viewModel.IsAssigned)
                return false;

            if (sourceActionType == UnitActionType.ResurrectAlly)
                return _viewModel.IsDefeated.CurrentValue && _viewModel.Side == source.Side;

            if (_viewModel.IsDefeated.CurrentValue)
                return false;

            if (sourceActionType == UnitActionType.DealDamage && _viewModel.Side != source.Side)
                return true;

            if (sourceActionType == UnitActionType.HealAlly && _viewModel.Side == source.Side)
            {
                if (source.Kind == UnitKind.Hero && source.SlotIndex == _viewModel.SlotIndex)
                    return false;

                return _viewModel.HPFill.CurrentValue < 1f;
            }

            if (sourceActionType == UnitActionType.SupportAlly && _viewModel.Side == source.Side)
                return true;

            return false;
        }

        public void SetSourceHighlight(bool active)
        {
            if (false == _glow)
                return;

            if (active && _config)
                _glow.color = _config.SourceHighlightColor;

            _glow.gameObject.SetActive(active);
        }

        public void SetTargetHighlight(bool active, UnitActionType actionType)
        {
            if (false == _glow)
                return;

            if (active && _config)
                _glow.color = GetTargetHighlightColor(actionType);

            _glow.gameObject.SetActive(active);
        }

        private Color GetTargetHighlightColor(UnitActionType actionType)
        {
            if (actionType == UnitActionType.ResurrectAlly)
                return _config.ResurrectTargetColor;

            return actionType is UnitActionType.HealAlly or UnitActionType.SupportAlly
                ? _config.HealTargetColor
                : _config.AttackTargetColor;
        }

        public void CaptureCurrentLayoutPose()
        {
            _knockbackTween?.Kill();
            _knockbackTween = null;
            _originalLocalPos = transform.localPosition;
        }
        

        private void BindPortrait(HeroSlotViewModel viewModel)
        {
            if (false == _portrait)
                return;

            ResetPortraitDeathFill();
            _portrait.enabled = viewModel.IsAssigned;
            _originalPortraitColor = _portrait.color;

            if (false == viewModel.IsAssigned)
                return;

            ApplySlotColor(viewModel.SlotColor);

            if (viewModel.Portrait)
                _portrait.sprite = viewModel.Portrait;

            ApplyAvailabilityPortraitState();
        }

        private void BindHPBars(HeroSlotViewModel viewModel)
        {
            if (false == viewModel.IsAssigned)
                return;

            ApplyHPVisualConfig();

            if (_hpBar)
                _hpBar.SnapFill(viewModel.HPFill.CurrentValue);

            if (_hpLagBar)
                _hpLagBar.SnapFill(viewModel.HPFill.CurrentValue);

            if (_hpText)
                _hpText.gameObject.SetActive(true);

            SetHPText(viewModel.CurrentHP, viewModel.MaxHP);

            viewModel.HealthBarUpdated
                .Subscribe(ApplyHealthBarUpdate)
                .AddTo(_disposables);
        }

        private void BindShieldBars(HeroSlotViewModel viewModel)
        {
            if (false == viewModel.IsAssigned)
            {
                SetShieldObjectsActive(false);
                return;
            }

            ApplyShieldVisualConfig();

            if (_shieldBar)
                _shieldBar.SnapFill(viewModel.ShieldFill.CurrentValue);

            if (_shieldLagBar)
                _shieldLagBar.SnapFill(viewModel.ShieldFill.CurrentValue);

            ApplyShieldText(viewModel.CurrentShield);
            SetShieldObjectsActive(viewModel.CurrentShield > 0);

            viewModel.ShieldBarUpdated
                .Subscribe(ApplyShieldBarUpdate)
                .AddTo(_disposables);
        }

        private void BindHitReaction(HeroSlotViewModel viewModel)
        {
            if (false == viewModel.IsAssigned || false == _portrait || false == _config)
                return;

            _originalPortraitColor = _portrait.color;

            viewModel.Hit
                .Subscribe(_ =>
                {
                    PlayHitFlash();
                    PlayKnockback();
                })
                .AddTo(_disposables);
        }

        private void BindDeathState(HeroSlotViewModel viewModel)
        {
            viewModel.IsDefeated
                .Subscribe(defeated =>
                {
                    var visuals = _deathConfig ? _deathConfig.HeroDeathVisuals : default;

                    if (defeated)
                        FinalizeDeathState(viewModel.HPFill.CurrentValue);

                    ApplyDeathColor(defeated);

                    if (visuals.ApplyDeathFill)
                        SetPortraitDeathFill(defeated);

                    if (null != _activateOnDeath)
                        for (var i = 0; i < _activateOnDeath.Length; i++)
                        {
                            var go = _activateOnDeath[i];
                            if (go) 
                                go.SetActive(defeated);
                        }

                    if (null != _deactivateOnDeath)
                        for (var i = 0; i < _deactivateOnDeath.Length; i++)
                        {
                            var go = _deactivateOnDeath[i];
                            if (go) 
                                go.SetActive(false == defeated);
                        }

                    ApplySlotKindPassiveObjectsState();
                })
                .AddTo(_disposables);
        }

        private void BindCooldownSweep(HeroSlotViewModel viewModel)
        {
            if (false == viewModel.IsAssigned)
                return;

            _cooldownSweep?.SetSprite(_portrait ? _portrait.sprite : null);
            _cooldownSweep?.SetCooldown(0f, 0f);

            viewModel.CooldownProgress
                .Subscribe(info =>
                {
                    _cooldownProgress = info;
                    ApplyCooldownSweep();
                })
                .AddTo(_disposables);

            viewModel.StunProgress
                .Subscribe(info =>
                {
                    _stunProgress = info;
                    ApplyCooldownSweep();
                })
                .AddTo(_disposables);
        }

        private void ApplyCooldownSweep()
        {
            if (_stunProgress.Remaining > 0f)
            {
                _cooldownSweep?.SetCooldown(_stunProgress.Remaining, _stunProgress.Duration,
                    _config ? _config.StunSweepColor : Color.blue);
                SetPortraitGrayscale(false);
                return;
            }

            _cooldownSweep?.SetCooldown(_cooldownProgress.Remaining, _cooldownProgress.Duration,
                _config ? _config.CooldownSweepColor : new Color(0f, 0f, 0f, 0.65f));
            SetPortraitGrayscale(_cooldownProgress.Remaining > 0f && _config && _config.CooldownGrayscaleEnabled);
        }

        private void BindSlotKindPassiveState(HeroSlotViewModel viewModel)
        {
            ApplySlotKindPassiveObjectsState();
            viewModel.IsSlotKindPassiveActive
                .Subscribe(_ => ApplySlotKindPassiveObjectsState())
                .AddTo(_disposables);
        }

        private void BindEnergyCostLabel(HeroSlotViewModel viewModel)
        {
            if (!_energyCostLabel)
                return;

            _energyCostLabel.text = $"{viewModel.ActivationEnergyCost}";
            viewModel.ActivationEnergyCostChanged
                .Subscribe(cost => _energyCostLabel.text = $"{cost}")
                .AddTo(_disposables);
        }

        private void BindAbilityPowerLabel(HeroSlotViewModel viewModel)
        {
            if (!_abilityPowerLabel)
                return;

            _abilityPowerTextTween?.Dispose();
            ApplyAbilityPowerLabelState(viewModel);
            _abilityPowerTextTween = new AnimatedIntegerText(_abilityPowerLabel);
            _abilityPowerTextTween.SetInstant(viewModel.AbilityPower);
            viewModel.AbilityPowerChanged
                .Subscribe(power =>
                {
                    ApplyAbilityPowerLabelState(viewModel);
                    if (viewModel.ShouldShowAbilityPower)
                        _abilityPowerTextTween.AnimateTo(power, AbilityPowerAnimDuration, Ease.OutQuad);
                })
                .AddTo(_disposables);
        }

        private void ApplyAbilityPowerLabelState(HeroSlotViewModel viewModel)
        {
            if (_abilityPowerLabel)
                _abilityPowerLabel.gameObject.SetActive(viewModel.ShouldShowAbilityPower);
        }

        private void ApplySlotKindPassiveObjectsState()
        {
            var defeated = _viewModel != null && _viewModel.IsDefeated.CurrentValue;
            var active = _viewModel != null && _viewModel.IsSlotKindPassiveActive.CurrentValue;

            SetObjectsActive(_activateOnSlotKindPassive, active && false == defeated);
            SetObjectsActive(_deactivateOnSlotKindPassive, false == active && false == defeated);
        }

        private static void SetObjectsActive(GameObject[] gameObjects, bool active)
        {
            if (null == gameObjects)
                return;

            for (var i = 0; i < gameObjects.Length; i++)
                if (gameObjects[i])
                    gameObjects[i].SetActive(active);
        }

        private void SetPortraitGrayscale(bool active)
        {
            if (false == _portrait)
                return;

            _portraitPropertyBlock ??= new MaterialPropertyBlock();
            _portraitPropertyBlock.SetFloat(GrayscaleEnabledShaderId, active ? 1f : 0f);
            _portrait.SetPropertyBlock(_portraitPropertyBlock);
        }

        private void BindAvailabilityState(HeroSlotViewModel viewModel)
        {
            viewModel.IsAvailabilityDimmed
                .Subscribe(dimmed =>
                {
                    _isAvailabilityDimmed = dimmed;
                    ApplyAvailabilityPortraitState();
                })
                .AddTo(_disposables);

            viewModel.IsDefeated
                .Subscribe(_ => ApplyAvailabilityPortraitState())
                .AddTo(_disposables);
        }

        private void ApplyHealthBarUpdate(HealthBarUpdate update)
        {
            SetHPText(update.CurrentHP, update.MaxHP);

            if (update.Mode == HealthBarUpdateMode.Snap)
            {
                _hpBar?.SnapFill(update.Fill);
                _hpLagBar?.SnapFill(update.Fill);
                
                return;
            }

            if (update.Mode == HealthBarUpdateMode.Damage)
            {
                _hpBar?.SetFill(update.Fill);

                if (_config)
                    _hpLagBar?.SetFillAnimated(update.Fill, _config.HPBarLagDuration, _config.HPBarLagDelay);
                else
                    _hpLagBar?.SetFill(update.Fill);

                return;
            }

            var healDuration = _config ? _config.HPBarHealDuration : 0.4f;
            _hpBar?.SetFillAnimated(update.Fill, healDuration);
            _hpLagBar?.SetFillAnimated(update.Fill, healDuration);
        }

        private void ApplyShieldBarUpdate(ShieldBarUpdate update)
        {
            ApplyShieldText(update.CurrentShield);
            SetShieldObjectsActive(update.CurrentShield > 0);

            if (update.Mode == HealthBarUpdateMode.Snap)
            {
                _shieldBar?.SnapFill(update.Fill);
                _shieldLagBar?.SnapFill(update.Fill);

                return;
            }

            if (update.Mode == HealthBarUpdateMode.Damage)
            {
                _shieldBar?.SetFill(update.Fill);

                if (_config)
                    _shieldLagBar?.SetFillAnimated(update.Fill, _config.HPBarLagDuration, _config.HPBarLagDelay);
                else
                    _shieldLagBar?.SetFill(update.Fill);

                return;
            }

            _shieldBar?.SnapFill(update.Fill);
            _shieldLagBar?.SnapFill(update.Fill);
        }

        private void SetHPText(int currentHP, int maxHP)
        {
            if (false == _hpText)
                return;

            _hpText.text = maxHP > 0 ? $"{currentHP}" : string.Empty;
        }

        private void ApplyHPVisualConfig()
        {
            if (false == _config)
                return;

            _hpBar?.SetFillColor(_config.HPBarColor);
            _hpLagBar?.SetFillColor(_config.HPBarLagColor);

            if (_hpText)
                _hpText.color = _config.HPTextColor;
        }

        private void ApplyShieldText(int currentShield)
        {
            if (_shieldText)
                _shieldText.text = currentShield > 0 ? $"{currentShield}" : string.Empty;
        }

        private void ApplyShieldVisualConfig()
        {
            if (false == _config)
                return;

            _shieldBar?.SetFillColor(_config.ShieldBarColor);
            _shieldLagBar?.SetFillColor(_config.ShieldBarLagColor);

            if (_shieldText)
                _shieldText.color = _config.ShieldTextColor;
        }

        private void SetShieldObjectsActive(bool active)
        {
            if (_shieldRoot)
            {
                _shieldRoot.SetActive(active);
                return;
            }

            if (_shieldBar)
                _shieldBar.gameObject.SetActive(active);

            if (_shieldLagBar)
                _shieldLagBar.gameObject.SetActive(active);

            if (_shieldText)
                _shieldText.gameObject.SetActive(active);
        }

        private void PlayHitFlash()
        {
            _hitFlashTween?.Kill();
            var halfDuration = _config.HitFlashDuration * 0.5f;

            _hitFlashTween = _portrait
                .DOColor(_config.HitFlashColor, halfDuration)
                .SetEase(_config.HitFlashEase)
                .OnComplete(() =>
                {
                    _hitFlashTween = _portrait
                        .DOColor(GetPortraitBaseColor(), halfDuration)
                        .SetEase(_config.HitFlashEase);
                });
        }

        private void PlayKnockback()
        {
            _knockbackTween?.Kill();
            transform.localPosition = _originalLocalPos;

            var direction = _viewModel.Side == BattleSide.Enemy ? 1f : -1f;
            var targetY = _originalLocalPos.y + direction * _config.KnockbackDistance;
            var halfDuration = _config.KnockbackDuration * 0.5f;

            _knockbackTween = transform
                .DOLocalMoveY(targetY, halfDuration)
                .SetEase(_config.KnockbackEase)
                .OnComplete(() =>
                {
                    _knockbackTween = transform
                        .DOLocalMoveY(_originalLocalPos.y, halfDuration)
                        .SetEase(_config.KnockbackEase);
                });
        }

        private void FinalizeDeathState(float hpFill)
        {
            _hitFlashTween?.Kill();
            _hitFlashTween = null;
            _knockbackTween?.Kill();
            _knockbackTween = null;
            transform.localPosition = _originalLocalPos;

            if (_portrait)
                _portrait.color = GetPortraitBaseColor();

            _hpBar?.SnapFill(hpFill);
            _hpLagBar?.SnapFill(hpFill);
        }

        private void SetPortraitDeathFill(bool active)
        {
            if (false == _portrait)
                return;

            _portraitPropertyBlock ??= new MaterialPropertyBlock();

            if (false == active)
            {
                _portraitPropertyBlock.Clear();
                _portrait.SetPropertyBlock(_portraitPropertyBlock);
                return;
            }

            _portraitPropertyBlock.Clear();
            _portraitPropertyBlock.SetFloat(FillEnabledShaderId, 1f);
            _portraitPropertyBlock.SetFloat(FillReplaceShaderId, 1f);
            _portrait.SetPropertyBlock(_portraitPropertyBlock);
        }

        private void ResetPortraitDeathFill()
        {
            SetPortraitDeathFill(false);
        }

        private void CacheDeathColors()
        {
            if (null == _deathColoredRenderers)
            {
                _originalDeathColors = null;
                return;
            }

            _originalDeathColors = new Color[_deathColoredRenderers.Length];
            for (var i = 0; i < _deathColoredRenderers.Length; i++)
                if (_deathColoredRenderers[i])
                    _originalDeathColors[i] = _deathColoredRenderers[i].color;
        }

        private void ApplyDeathColor(bool defeated)
        {
            if (null == _deathColoredRenderers || null == _originalDeathColors)
                return;

            var deathColor = _deathConfig ? _deathConfig.HeroDeathVisuals.DeathColor : Color.white;

            for (var i = 0; i < _deathColoredRenderers.Length; i++)
                if (_deathColoredRenderers[i])
                    _deathColoredRenderers[i].color = defeated ? deathColor : _originalDeathColors[i];
        }

        private void ApplySlotColor(Color color)
        {
            if (null == _energyColoredRenderers)
                return;

            for (var i = 0; i < _energyColoredRenderers.Length; i++)
                if (_energyColoredRenderers[i])
                    _energyColoredRenderers[i].color = color;
        }

        private void ApplyAvailabilityPortraitState()
        {
            if (false == _portrait)
                return;

            _portrait.color = GetPortraitBaseColor();
        }

        private Color GetPortraitBaseColor()
        {
            if (null == _viewModel || _viewModel.IsDefeated.CurrentValue || false == _isAvailabilityDimmed)
                return _originalPortraitColor;

            return new Color(
                _originalPortraitColor.r * DisabledPortraitBrightness,
                _originalPortraitColor.g * DisabledPortraitBrightness,
                _originalPortraitColor.b * DisabledPortraitBrightness,
                _originalPortraitColor.a);
        }
    }
}