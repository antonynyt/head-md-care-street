# Unity 

The interactive experience is developed on Unity. 

- **Unity project folder is** `/unity/final-thecafe`.
- `2026-05-20-Greyboxing` and `2026-05-22-cupinteraction` are old iterations.

# Interaction

The interaction consist of long pressing. (1) you break a bike (2) you pour cafe in a cup. The time devoted to each conversation depends on the simple act of pouring a glass of a hot drink: the longer it is filled, the more the story you get.

# Todo

- [x] Implement new dialogs recordings in Unity
- [ ] Add music & sound design

### Technical bugs

- [ ] Interrupt (fadeout) and avoid (pause) new line to be played when the player press the brakes. On bike scene (biker thoughts).
- [ ] The questions (cup filling) dialog (from bike to roberto) doesn't wait the end of the question and interrupt the next sentence. e.g. bike dialog gets cut by roberto line. It should stack and play til the end.
- [ ] Nice to have (but UX need): Add a small hint on the brakes (glowing?) to indicate it's interactive


# Good to know

- The audio files drives the interaction/conversation flow, timing, the end of the audio triggers the next dialog file. The story is written with Yarn and the line tags correspond to the audio filename.
- Uses animatorController for cup zooms and camera animation control, repeated in each scene. Same for bike brake animation, but bike scene uses one scene.
- On the cup scenes, an InteractionDirector gameObject manages the interface between yarn and unity cup filling steps and emptying. You need to fill the yarn nodes in the fields