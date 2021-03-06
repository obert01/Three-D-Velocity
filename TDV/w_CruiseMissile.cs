/* This source is provided under the GNU AGPLv3  license. You are free to modify and distribute this source and any containing work (such as sound files) provided that:
* - You make available complete source code of modifications, even if the modifications are part of a larger project, and make the modified work available under the same license (GNU AGPLv3).
* - You include all copyright and license notices on the modified source.
* - You state which parts of this source were changed in your work
* Note that containing works (such as SharpDX) may be available under a different license.
* Copyright (C) Munawar Bijani
*/
using System;
using System.IO;
using BPCSharedComponent.ExtendedAudio;
using BPCSharedComponent.VectorCalculation;
using BPCSharedComponent.ExtendedAudio;
using BPCSharedComponent.Input;
namespace TDV
{
	public class CruiseMissile : WeaponBase
	{
		private ExtendedAudioBuffer launchSound;
		private ExtendedAudioBuffer fox;
		private ExtendedAudioBuffer missileSound;
		private ExtendedAudioBuffer hitSound;
		private long m_time;


		public CruiseMissile(Weapons w)
			: base(w, "p" + (int)WeaponTypes.cruiseMissile)
		{
			type = WeaponTypes.cruiseMissile;
			weapon.decreaseAmmunitionFor(WeaponTypes.cruiseMissile);
			neutralizeSpeed((weapon.creator.flyingCruiseMissile) ? 1800f : 900f);
			setSpan(0.10f, 0.25f);
			launchSound = loadSound(soundPath + "cr1.wav");
			missileSound = DSound.LoadSound(DSound.SoundPath + "\\cr2.wav");
		}

		public override void free()
		{
			base.free();
			DSound.unloadSound(ref launchSound);
			DSound.unloadSound(ref missileSound);
			DSound.unloadSound(ref hitSound);
			DSound.unloadSound(ref fox);
		}

		public override void lockOn(Projector target)
		{
			origTarget = target;
		}

		public override void onTick()
		{
			if (isFinished())
			{
				fireDisposeEvent();
				return;
			}
			if (finished && performing)
			{
				//The weapon is done doing what it needs to do, but a sound is still playing.
				//Do not free this weapon until the sound is done playing or the program will act up.
				if (hitSound != null)
					performing = DSound.isPlaying(hitSound);
				return;
			}

			performing = true;
			if ((Math.Abs(Environment.TickCount - time)) / 1000 < 2)
				return;
			playSound3d(missileSound, false, true);
			if (!inVerticalRange(10))
			{
				if (z > origTarget.z)
					z -= 10;
				if (z < origTarget.z)
					z += 10;
				if (Math.Abs(z - origTarget.z) <= 10)
					z = origTarget.z;
			}
			direction = Degrees.GetDegreesBetween(x, y, origTarget.x, origTarget.y);
			base.onTick();
			if (inFiringRange())
			{
				missileSound.stop();
				hitSound = target.loadSound(target.soundPath + "m3-" + Common.getRandom(1, 2) + ".wav");
				target.playSound(hitSound, true, false);
				// Since the boss aircraft has 10,000 damage, let's not let the player kill them with one cruise missile.
				// However, the missile should wipe out everything else it contacts.
				fireHitEvent(target, (target is JuliusAircraft)? 1500:100000);
				finished = true;
				return;
			}
			if (totalDistance > 30.0 || !Weapons.isValidLock(origTarget) || finished)
			{
				missileSound.stop();
				finished = true;
				performing = (hitSound != null && DSound.isPlaying(hitSound)) || (expl != null && DSound.isPlaying(expl));
			}
		}

		public override void serverSideHit(Projector target, int damageAmount)
		{
			missileSound.stop();
			hitSound = target.loadSound(target.soundPath + "m3-" + Common.getRandom(1, 2) + ".wav");
			target.playSound(hitSound, true, false);
			fireHitEvent(target, damageAmount);
			finished = true;
		}

		public override void use()
		{
			direction = Degrees.GetDegreesBetween(x, y, origTarget.x, origTarget.y);
			z = origTarget.z;
			time = Environment.TickCount;
			playSound(launchSound, true, false);
			if (isAI)
			{
				if (origTarget is Aircraft)
					((Aircraft)origTarget).notifyOf(Notifications.missileLaunch, true);
				return;
			}
			DXInput.startCruiseMissileEffect();
			fox = loadSound(soundPath + "fox3.wav");
			playSound(fox, true, false);
		}

		private long time
		{
			get { return (m_time); }
			set { m_time = value; }
		}

		public override void save(BinaryWriter w)
		{
			base.save(w);
			w.Write(time);
		}
		public override bool load()
		{
			if (!base.load())
				return false;
			BinaryReader r = Common.inFile;
			time = r.ReadInt64();
			return true;
		}
	}
}
