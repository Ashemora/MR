using DG.Tweening;
using Cysharp.Threading.Tasks;
using Project.Scripts.Configs.Battle.Units;
using Project.Scripts.Configs.Battle.Visuals;
using Project.Scripts.Configs.UI;
using Project.Scripts.Gameplay.Battle.Targeting;
using Project.Scripts.Gameplay.UI;
using Project.Scripts.Services.Combat.Units;
using R3;
using TMPro;
using UnityEngine;
using Project.Scripts.Shared.Units;

namespace Project.Scripts.Gameplay.Battle.Units
{
    public class AvatarSlotView : MonoBehaviour, ITargetable
    {
        private static readonly int FillEnabledShaderId = Shader.PropertyToID("_FillEnabled");
        private static readonly int FillReplaceShaderId = Shader.PropertyToID("_FillReplace");
        private static readonly int GrayscaleEnabledShaderId = Shader.PropertyToID("_GrayscaleEnabled");
        private const float DisabledPortraitBrightness = 0.45f;
        private const float AbilityPowerAnimDuration = 0.25f;

        
        [Tooltip("SpriteRenderer, определяющий границы слота для таргетинга; не используется для окраски")]
        [SerializeField] private SpriteRenderer _boundsSource;

        [Space(10)]
        [Tooltip("SpriteRenderers, которые красятся цветом аватара")]
        [SerializeField] private SpriteRenderer[] _energyColoredRenderers;

        [Space(10)]
        [Tooltip("SpriteRenderers, которые красятся в DeathColor из UnitDeathConfig при гибели аватара")]
        [SerializeField] private SpriteRenderer[] _deathColoredRenderers;

        [Space(10)]
        [Tooltip("SpriteRenderer портрета - отображает изображение аватара")]
        [SerializeField] private SpriteRenderer _portrait;

        [Tooltip("Radial cooldown overlay - shown on top of portrait during cooldown")]
        [SerializeField] private CooldownSweepView _cooldownSweep;

        [Tooltip("SpriteRenderer свечения с материалом Additive - отображается как подсветка источника или цели")]
        [SerializeField] private SpriteRenderer _glow;

        [Tooltip("Основная полоса HP - мгновенно обновляется при получении урона")]
        [SerializeField] private BarRenderer _hpBar;

        [Tooltip("Лаг-полоса HP позади основной полосы - опустошается с задержкой после получения урона")]
        [SerializeField] private BarRenderer _hpLagBar;

        [Tooltip("Текст HP (только текущее значение) - скрывается при MaxHP = 0 (бессмертный юнит)")]
        [SerializeField] private TMP_Text _hpText;

        [Tooltip("Текст стоимости активации способности в единицах энергии")]
        [SerializeField] private TMP_Text _energyCostLabel;

        [Tooltip("Текст величины силы способности (урон / лечение)")]
        [SerializeField] private TMP_Text _abilityPowerLabel;

        [Tooltip("Transform, используемый как якорь для всплывающих чисел урона/лечения")]
        [SerializeField] private Transform _hitAnchor;

        [Tooltip("Якорь для прилёта орбов передачи энергии - по умолчанию HitAnchor, если не назначен")]
        [SerializeField] private Transform _energyAnchor;

        [Space(10)]
        [Tooltip("GameObject-ы, которые активируются при смерти аватара")]
        [SerializeField] private GameObject[] _activateOnDeath;

        [Space(10)]
        [Tooltip("GameObject-ы, которые деактивируются при смерти аватара")]
        [SerializeField] private GameObject[] _deactivateOnDeath;


        public UnitDescriptor Descriptor => UnitDescriptor.Avatar(_viewModel.Side, _viewModel.AbilityType);
        public bool IsReadySource => _viewModel is { Side: BattleSide.Player } && _viewModel.EnergyBar.IsReady.CurrentValue;
        public Bounds WorldBounds => _boundsSource ? _boundsSource.bounds : new Bounds(transform.position, Vector3.one);
        public Transform HitAnchor => _hitAnchor ? _hitAnchor : transform;
        public Transform EnergyAnchor => _energyAnchor ? _energyAnchor : HitAnchor;


        private AvatarSlotViewModel _viewModel;
        private IAvatarGroupDefenseService _groupDefense;
        private BattleAnimationConfig _config;
        private UnitDeathConfig _deathConfig;
        private CompositeDisposable _disposables;
        private Color _originalPortraitColor;
        private Vector3 _originalLocalPos;
        private Vector3 _originalLocalScale;
        private Color[] _originalDeathColors;
        private Tween _hitFlashTween;
        private Tween _knockbackTween;
        private Tween _resultPulseTween;
        private AnimatedIntegerText _abilityPowerTextTween;
        private MaterialPropertyBlock _portraitPropertyBlock;
        private bool _isAvailabilityDimmed;


        private void OnDestroy()
        {
            _hitFlashTween?.Kill();
            _knockbackTween?.Kill();
            _resultPulseTween?.Kill();
            _abilityPowerTextTween?.Dispose();
            _disposables?.Dispose();
        }


        public void Bind(AvatarSlotViewModel viewModel, IAvatarGroupDefenseService groupDefense, UnitDeathConfig deathConfig)
        {
            _viewModel = viewModel;
            _groupDefense = groupDefense;
            _config = viewModel.AnimConfig;
            _deathConfig = deathConfig;
            _disposables?.Dispose();
            _disposables = new CompositeDisposable();
            _originalLocalPos = transform.localPosition;
            _originalLocalScale = transform.localScale;
            ResetPortraitDeathFill();
            CacheDeathColors();

            BindPortrait(viewModel);
            BindHPBars(viewModel);
            BindHitReaction(viewModel);
            BindDeathState(viewModel);
            BindAvailabilityState(viewModel);
            BindCooldownSweep(viewModel);
            if (_energyCostLabel)
                _energyCostLabel.text = $"{viewModel.ActivationEnergyCost}";

            BindAbilityPowerLabel(viewModel);
        }

        public async UniTask PlayResultPulse(AvatarPulseStepConfig config)
        {
            if (null == config)
                return;

            if (config.Duration <= 0f || config.ScaleMultiplier <= 0f)
                return;

            _resultPulseTween?.Kill();
            transform.localScale = _originalLocalScale;

            var halfDuration = config.Duration * 0.5f;
            var targetScale = _originalLocalScale * config.ScaleMultiplier;

            _resultPulseTween = DOTween.Sequence()
                .Append(transform.DOScale(targetScale, halfDuration).SetEase(config.Ease))
                .Append(transform.DOScale(_originalLocalScale, halfDuration).SetEase(config.Ease))
                .OnKill(() =>
                {
                    if (this)
                        transform.localScale = _originalLocalScale;

                    _resultPulseTween = null;
                })
                .OnComplete(() =>
                {
                    transform.localScale = _originalLocalScale;
                    _resultPulseTween = null;
                });

            await _resultPulseTween.ToUniTask();
        }

        public bool IsValidTarget(UnitDescriptor source)
        {
            if (null == _viewModel || _viewModel.IsDefeated.CurrentValue)
                return false;

            if (source.ActionType == HeroActionType.DealDamage && _viewModel.Side == BattleSide.Enemy)
            {
                if (null != _groupDefense && false == _groupDefense.IsExposed(BattleSide.Enemy))
                    return false;

                return true;
            }

            if (source.ActionType == HeroActionType.HealAlly && _viewModel.Side == BattleSide.Player)
            {
                if (source.Kind == UnitKind.Avatar)
                    return false;

                return _viewModel.HPFill.CurrentValue < 1f;
            }

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

        public void SetTargetHighlight(bool active, HeroActionType actionType)
        {
            if (false == _glow)
                return;

            if (active && _config)
                _glow.color = actionType == HeroActionType.HealAlly
                    ? _config.HealTargetColor
                    : _config.AttackTargetColor;

            _glow.gameObject.SetActive(active);
        }

        public void CaptureCurrentLayoutPose()
        {
            _knockbackTween?.Kill();
            _knockbackTween = null;
            _resultPulseTween?.Kill();
            _resultPulseTween = null;
            _originalLocalPos = transform.localPosition;
            _originalLocalScale = transform.localScale;
        }


        private void BindPortrait(AvatarSlotViewModel viewModel)
        {
            ApplySlotColor(viewModel.SlotColor);

            if (false == _portrait)
                return;

            ResetPortraitDeathFill();
            _portrait.sprite = viewModel.Portrait;
            _originalPortraitColor = _portrait.color;
            ApplyAvailabilityPortraitState();
        }

        private void BindHPBars(AvatarSlotViewModel viewModel)
        {
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

        private void BindHitReaction(AvatarSlotViewModel viewModel)
        {
            if (false == _portrait || false == _config)
                return;

            viewModel.Hit
                .Subscribe(_ =>
                {
                    PlayHitFlash();
                    PlayKnockback();
                })
                .AddTo(_disposables);
        }

        private void BindDeathState(AvatarSlotViewModel viewModel)
        {
            viewModel.IsDefeated
                .Subscribe(defeated =>
                {
                    var visuals = _deathConfig ? _deathConfig.AvatarDeathVisuals : default;

                    if (defeated)
                        FinalizeDeathState(viewModel.HPFill.CurrentValue);

                    ApplyDeathColor(defeated);

                    if (visuals.ApplyDeathFill)
                        SetPortraitDeathFill(defeated);

                    if (_activateOnDeath != null)
                        foreach (var go in _activateOnDeath)
                            if (go) go.SetActive(defeated);

                    if (_deactivateOnDeath != null)
                        foreach (var go in _deactivateOnDeath)
                            if (go) go.SetActive(false == defeated);
                })
                .AddTo(_disposables);
        }

        private void BindCooldownSweep(AvatarSlotViewModel viewModel)
        {
            _cooldownSweep?.SetSprite(_portrait ? _portrait.sprite : null);
            _cooldownSweep?.SetCooldown(0f, 0f);

            viewModel.EnergyBar.CooldownProgress
                .Subscribe(info =>
                {
                    _cooldownSweep?.SetCooldown(info.Remaining, info.Duration);
                    SetPortraitGrayscale(info.Remaining > 0f && _config && _config.CooldownGrayscaleEnabled);
                })
                .AddTo(_disposables);
        }

        private void BindAbilityPowerLabel(AvatarSlotViewModel viewModel)
        {
            if (!_abilityPowerLabel)
                return;

            _abilityPowerTextTween?.Dispose();
            _abilityPowerTextTween = new AnimatedIntegerText(_abilityPowerLabel);
            _abilityPowerTextTween.SetInstant(viewModel.AbilityPower);
            viewModel.AbilityPowerChanged
                .Subscribe(power => _abilityPowerTextTween.AnimateTo(power, AbilityPowerAnimDuration, Ease.OutQuad))
                .AddTo(_disposables);
        }

        private void SetPortraitGrayscale(bool active)
        {
            if (false == _portrait)
                return;

            _portraitPropertyBlock ??= new MaterialPropertyBlock();
            _portraitPropertyBlock.SetFloat(GrayscaleEnabledShaderId, active ? 1f : 0f);
            _portrait.SetPropertyBlock(_portraitPropertyBlock);
        }

        private void BindAvailabilityState(AvatarSlotViewModel viewModel)
        {
            viewModel.EnergyBar.IsAvailabilityDimmed
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

        private void SetHPText(int currentHP, int maxHP)
        {
            if (false == _hpText)
                return;

            _hpText.text = maxHP > 0 ? $"{currentHP}" : string.Empty;
        }

        private void FinalizeDeathState(float hpFill)
        {
            _hitFlashTween?.Kill();
            _hitFlashTween = null;
            _knockbackTween?.Kill();
            _knockbackTween = null;
            _resultPulseTween?.Kill();
            _resultPulseTween = null;
            transform.localPosition = _originalLocalPos;
            transform.localScale = _originalLocalScale;

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
            if (_deathColoredRenderers == null)
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
            if (_deathColoredRenderers == null || _originalDeathColors == null)
                return;

            var deathColor = _deathConfig ? _deathConfig.AvatarDeathVisuals.DeathColor : Color.white;

            for (var i = 0; i < _deathColoredRenderers.Length; i++)
                if (_deathColoredRenderers[i])
                    _deathColoredRenderers[i].color = defeated ? deathColor : _originalDeathColors[i];
        }

        private void ApplySlotColor(Color color)
        {
            if (_energyColoredRenderers == null)
                return;

            for (var i = 0; i < _energyColoredRenderers.Length; i++)
                if (_energyColoredRenderers[i])
                    _energyColoredRenderers[i].color = color;
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

        private void ApplyAvailabilityPortraitState()
        {
            if (false == _portrait)
                return;

            _portrait.color = GetPortraitBaseColor();
        }

        private Color GetPortraitBaseColor()
        {
            if (_viewModel == null || _viewModel.IsDefeated.CurrentValue || false == _isAvailabilityDimmed)
                return _originalPortraitColor;

            return new Color(
                _originalPortraitColor.r * DisabledPortraitBrightness,
                _originalPortraitColor.g * DisabledPortraitBrightness,
                _originalPortraitColor.b * DisabledPortraitBrightness,
                _originalPortraitColor.a);
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
    }
}