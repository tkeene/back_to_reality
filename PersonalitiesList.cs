using Godot;
using System;

public class PersonalitiesList : Node
{
    public static string[] personalitiesList =
    {
        "Angry;\"I HATE THIS PLACE!\"",
        "Sad;*Cries*",
        "Quick;\"Woohoo!\"",
        "Confused;\"Quanta---whuuut\"",
        "Sick;*BLECH*",
        "Heroic;\"Just call me SUPER-NAUT!\"",
        "Depressed;\"Am I in an indie game?\"",
        "Thoughtful;\"Hmm...If I repair the...\"",
        "Precise;\"Measure twice, cut once\"",
        "Cheerful;\"Hey team! We got this!\"",
        "Cruel;\"You're the ugly one, huh.\"",
        "Brave;\"No fear!\"",
        "Dramatic;\"Alas poor Quantumnaut!\"",
        "Friendly;\"Hi myself!\"",
        "Optimistic;\"I can do this!\"",
        "Forgetful;\"Wait, why am I here?\"",
        "Cold-Blooded;\"Ends justify the means\"",
        "Coward;\"Noooooope\"",
        "Crazy;\"Would you like some toast?\"",
        "Excitable;*HIGH FIVE*",
        "Neurotic;\"No! The colors have to match\"",
        "Daredevil;\"Here we gooooo\"",
        "Anxious;\"Oh boy, oh boy, oh boy.\"",
        "Detached;\"Eh.\"",
        "Rude;\"Listen here smeg-for-breath\"",
        "Focused;\"Alright, let's do it.\"",
        "Short Fuse;\"Hurry up!\"",
        "Patient;\"Take our time\"",
        "Emotional;\"Feelings\"",
        "Hyper;*running noises*",
        "Stressed;\"So much pressure.\"",
        "Super-Hyper;*ZOOM ZOOM*",
        "Impulsive;\"What if I push this...\"",
        "Joyless;\"Even if we survive, why bother.\"",
        "Moody;\"...\"",
        "Open-minded;\"Hm, I hadn't considered that\"",
        "Pessimistic;\"There's no way I can do this.\"",
        "Gentle;\"Careful now\"",
        "Practical;\"Let's think this through.\"",
        "Relaxed;\"It's all good dude.\"",
        "Sensitive;\"Why would you say that!?\"",
        "Serious;\"Focus up!\"",
        "Calm;\"Don't stress.\"",
        "Cocky;\"I got this.\"",
        "Confident;\"We got this.\"",
        "Loving;\"I love you fam.\"",
        "Timid;\"hi.\"",
        "Unsatisified;\"We can do better.\"",
        "Ignorant;\"You studied what?\"",
        "Funny;\"So I says to him..."
    };

    public static string GetNextPersonality(int currentClonePersonality)
    {
        int index = currentClonePersonality % personalitiesList.Length;
        return personalitiesList[index];
    }
}
