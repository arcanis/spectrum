using System;
using System.Collections.Generic;
using Spectrum.Components;

namespace Spectrum.States
{
    public class Start : States.Menu
    {
        public Start()
            : base(new States.Game(), "SPECTRUM", true)
        {
            this.AddAction("start", delegate() {
                SoundPlayer.PlayEffect(SoundEffectType.MenuItemSelectionClicked);

                int nbInputs = InputController.Instance.GetNumberOfAvailableInputs();

                if (nbInputs <= 1) {
                    SoundPlayer.IncreaseMainGameSongVolume();
                    return new States.Game();
                }

                return new States.PlayersSelect(this, nbInputs);
            });

            #if WINDOWS
                this.AddAction("switch fullscreen", delegate () {
                    SoundPlayer.PlayEffect(SoundEffectType.MenuItemSelectionClicked);

                    Application.Instance.GraphicsDeviceManager.ToggleFullScreen();

                    return null;
                });
            #endif

            this.AddAction("help", delegate() {
                return new States.Tutorial(this);
            });

            this.AddAction("exit", delegate() {
                SoundPlayer.PlayEffect(SoundEffectType.MenuItemSelectionChange); // click sound gets cut off

                return new States.Exit();
            });
        }

        public override void Initialize()
        {
            mPreviousState.Initialize();
            base.Initialize();
        }
    }
}
