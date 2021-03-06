﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Bot.Modules
{
    class trivia : Module
    {
        List<trivia_info> trivias = new List<trivia_info>();
        public readonly object trivialock = new object();
        public override void control(bot ircbot, BotConfig Conf, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            if (type.Equals("channel") && bot_command == true)
            {
                foreach (Command tmp_command in this.Commands)
                {
                    bool blocked = tmp_command.Blacklist.Contains(channel) || tmp_command.Blacklist.Contains(nick);
                    bool cmd_found = false;
                    bool spam_check = ircbot.get_spam_check(channel, nick, tmp_command.Spam_Check);
                    if (spam_check == true)
                    {
                        blocked = blocked || ircbot.get_spam_status(channel);
                    }
                    cmd_found = tmp_command.Triggers.Contains(command);
                    if (blocked == true && cmd_found == true)
                    {
                        ircbot.sendData("NOTICE", nick + " :I am currently too busy to process that.");
                    }
                    if (blocked == false && cmd_found == true)
                    {
                        foreach (string trigger in tmp_command.Triggers)
                        {
                            switch (trigger)
                            {
                                case "trivia":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        bool score_found = false;
                                        lock (trivialock)
                                        {
                                            foreach (trivia_info trivia in trivias)
                                            {
                                                if (trivia.channel.Equals(channel, StringComparison.InvariantCultureIgnoreCase) && trivia.running)
                                                {
                                                    score_found = true;
                                                    break;
                                                }
                                            }
                                        }
                                        if (!score_found)
                                        {
                                            ircbot.sendData("PRIVMSG", channel + " :Lets play some Trivia!");
                                            new_question(channel, ircbot);
                                        }
                                        else
                                        {
                                            ircbot.sendData("PRIVMSG", channel + " :There is already a trivia game started!");
                                        }
                                    }
                                    break;
                                case "scores":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        bool score_found = false;
                                        lock (trivialock)
                                        {
                                            foreach (trivia_info trivia in trivias)
                                            {
                                                if (trivia.channel.Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    score_found = true;
                                                    int index = 1;
                                                    string msg = "";
                                                    foreach (List<string> score in trivia.scores)
                                                    {
                                                        if (index <= 10)
                                                        {
                                                            msg += "[" + index + "] " + score[0] + ": " + score[1] + " points | ";
                                                        }
                                                        else
                                                        {
                                                            break;
                                                        }
                                                        index++;
                                                    }
                                                    if (!String.IsNullOrEmpty(msg))
                                                    {
                                                        ircbot.sendData("PRIVMSG", channel + " :" + msg.Trim().TrimEnd('|').Trim());
                                                    }
                                                    else
                                                    {
                                                        ircbot.sendData("PRIVMSG", channel + " :No scores are available.");
                                                    }
                                                    break;
                                                }
                                            }
                                        }
                                        if (!score_found)
                                        {
                                            ircbot.sendData("PRIVMSG", channel + " :No scores are available.");
                                        }
                                    }
                                    break;
                                case "score":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        bool score_found = false;
                                        lock (trivialock)
                                        {
                                            foreach (trivia_info trivia in trivias)
                                            {
                                                if (trivia.channel.Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    score_found = true;
                                                    int index = 0;
                                                    bool nick_found = false;
                                                    foreach (List<string> score in trivia.scores)
                                                    {
                                                        if (score[0].Equals(nick, StringComparison.InvariantCultureIgnoreCase))
                                                        {
                                                            ircbot.sendData("PRIVMSG", channel + " :You are rank " + (index + 1).ToString() + " out of " + trivia.scores.Count + " with " + score[1] + " points");
                                                            nick_found = true;
                                                            break;
                                                        }
                                                        index++;
                                                    }
                                                    if (!nick_found)
                                                    {
                                                        ircbot.sendData("NOTICE", nick + " :You are not currently ranked.  Try answering some questions to earn points!");
                                                    }
                                                    break;
                                                }
                                            }
                                        }
                                        if (!score_found)
                                        {
                                            ircbot.sendData("PRIVMSG", channel + " :No scores are available.");
                                        }
                                    }
                                    break;
                                case "stoptrivia":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        bool trivia_found = false;
                                        lock (trivialock)
                                        {
                                            foreach (trivia_info trivia in trivias)
                                            {
                                                if (trivia.channel.Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    trivia_found = true;
                                                    trivia.answer_timer.Stop();
                                                    trivia.running = false;
                                                    ircbot.sendData("PRIVMSG", channel + " :Trivia has been stopped!");
                                                    int index = 1;
                                                    List<List<string>> sorted = new List<List<string>>();
                                                    string msg = "";
                                                    foreach (List<string> score in trivia.scores)
                                                    {
                                                        if (index <= 10)
                                                        {
                                                            msg += "[" + index + "] " + score[0] + ": " + score[1] + " points | ";
                                                        }
                                                        else
                                                        {
                                                            break;
                                                        }
                                                        index++;
                                                    }
                                                    if (!String.IsNullOrEmpty(msg))
                                                    {
                                                        ircbot.sendData("PRIVMSG", channel + " :" + msg.Trim().TrimEnd('|').Trim());
                                                    }
                                                    trivia.scores = new List<List<string>>();
                                                    break;
                                                }
                                            }
                                        }
                                        if (!trivia_found)
                                        {
                                            ircbot.sendData("PRIVMSG", channel + " :There isn't a trivia game running here.");
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }
            }
            if(type.Equals("channel") && !bot_command)
            {
                bool won = false;
                lock (trivialock)
                {
                    foreach (trivia_info trivia in trivias)
                    {
                        if (trivia.channel.Equals(channel, StringComparison.InvariantCultureIgnoreCase) && trivia.running)
                        {
                            string response = "";
                            if (line.GetUpperBound(0) > 3)
                            {
                                response = line[3].TrimStart(':') + " " + line[4];
                            }
                            else
                            {
                                response = line[3].TrimStart(':');
                            }
                            string[] answers = trivia.answer.Split('*');
                            foreach (string answer in answers)
                            {
                                if (response.Equals(answer, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    int points = 0;
                                    int index = 0;
                                    won = true;
                                    foreach (List<string> score in trivia.scores)
                                    {
                                        if (score[0].Equals(nick, StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            int old_points = Convert.ToInt32(score[1]);
                                            points = old_points + 1;
                                            score[1] = points.ToString();
                                            break;
                                        }
                                        index++;
                                    }
                                    if (points > 0)
                                    {
                                        ircbot.sendData("PRIVMSG", channel + " :\"" + response + "\" is correct! " + nick + " now has " + points + " points!");
                                    }
                                    else
                                    {
                                        List<string> tmp_score = new List<string>();
                                        tmp_score.Add(nick);
                                        tmp_score.Add("1");
                                        trivia.scores.Add(tmp_score);
                                        points = 1;
                                        ircbot.sendData("PRIVMSG", channel + " :\"" + response + "\" is correct! " + nick + " now has " + points + " point!");
                                    }
                                    trivia.questions_answered++;
                                    trivia.answer_timer.Stop();

                                    List<List<string>> top = new List<List<string>>();
                                    List<List<string>> tmp_top = new List<List<string>>();
                                    tmp_top = trivia.scores;
                                    for (int x = 0; x < trivia.scores.Count; x++)
                                    {
                                        if (tmp_top.Count > 0)
                                        {
                                            int score_index = 0;
                                            int tmp_top_score = 0;
                                            int top_score_index = 0;
                                            bool found = false;
                                            foreach (List<string> top_score in tmp_top)
                                            {
                                                if (Convert.ToInt32(top_score[1]) > tmp_top_score)
                                                {
                                                    found = true;
                                                    tmp_top_score = Convert.ToInt32(top_score[1]);
                                                    top_score_index = score_index;
                                                }
                                                score_index++;
                                            }
                                            if (found)
                                            {
                                                top.Add(tmp_top[top_score_index]);
                                                tmp_top.RemoveAt(top_score_index);
                                                x--;
                                            }
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    trivia.scores = top;
                                }
                                break;
                            }
                            break;
                        }
                    }
                }
                if (won)
                {
                    new_question(channel, ircbot);
                }
            }
        }

        private void new_question(string channel, bot ircbot)
        {
            lock (trivialock)
            {
                bool trivia_found = false;
                string question_file = ircbot.cur_dir + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "trivia" + Path.DirectorySeparatorChar + "questions.txt";
                if (File.Exists(question_file))
                {
                    foreach (trivia_info trivia in trivias)
                    {
                        if (trivia.channel.Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                        {
                            trivia_found = true;
                            trivia.questions_asked++;
                            string[] questions = System.IO.File.ReadAllLines(question_file);
                            int line_count = questions.GetUpperBound(0);
                            Random random = new Random();
                            int index = random.Next(0, line_count + 1);
                            char[] sep = new char[] { '*' };
                            string[] parts = questions[index].Split(sep, 2);
                            trivia.question = parts[0];
                            trivia.answer = parts[1];
                            ircbot.sendData("PRIVMSG", channel + " :" + parts[0]);
                            trivia.answer_timer.Start();
                            trivia.running = true;
                            break;
                        }
                    }
                    if (!trivia_found)
                    {
                        trivia_info new_trivia = new trivia_info();
                        new_trivia.channel = channel;
                        new_trivia.answer_timer = new System.Timers.Timer();
                        new_trivia.answer_timer.Interval = 30000;
                        new_trivia.answer_timer.Enabled = true;
                        new_trivia.answer_timer.Elapsed += (sender, e) => answer_timer_Elapsed(channel, ircbot);
                        string[] questions = System.IO.File.ReadAllLines(question_file);
                        int line_count = questions.GetUpperBound(0);
                        Random random = new Random();
                        int index = random.Next(0, line_count + 1);
                        char[] sep = new char[] { '*' };
                        string[] parts = questions[index].Split(sep, 2);
                        new_trivia.question = parts[0];
                        new_trivia.answer = parts[1];
                        ircbot.sendData("PRIVMSG", channel + " :" + parts[0]);
                        new_trivia.answer_timer.Start();
                        new_trivia.running = true;
                        new_trivia.scores = new List<List<string>>();
                        trivias.Add(new_trivia);
                    }
                }
            }
        }

        private object answer_timer_Elapsed(string channel, bot ircbot)
        {
            bool trivia_found = false;
            lock (trivialock)
            {
                foreach (trivia_info trivia in trivias)
                {
                    if (trivia.channel.Equals(channel, StringComparison.InvariantCultureIgnoreCase))
                    {
                        ircbot.sendData("PRIVMSG", channel + " :Times up!  The answer was: " + trivia.answer.Replace("*", " or "));
                        trivia.answer_timer.Stop();
                        trivia_found = true;
                        break;
                    }
                }
            }
            if(trivia_found)
            {
                new_question(channel, ircbot);
            }
            return true;
        }
    }

    class trivia_info
    {
        public System.Timers.Timer answer_timer { get; set; }
        public bool running { get; set; }
        public string channel { get; set; }
        public string question { get; set; }
        public string answer { get; set; }
        public int questions_asked { get; set; }
        public int questions_answered { get; set; }
        public List<List<string>> scores { get; set; } 
    }
}
