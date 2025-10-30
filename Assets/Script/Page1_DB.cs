using UnityEngine;
using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using TMPro;
using UnityEngine.UI;

public class Page1_DB : MonoBehaviour
{
    // Connection details
    private string host = "deu.duraka.shop";
    private string port = "4264";
    private string sid = "xe";
    private string userid = "TEAM4";
    private string password = "Team4Tris";

    [Header("Input Object")]
    public TMP_InputField username;
    public TMP_InputField birth;
    public TMP_InputField sex;
    public TMP_InputField tel;
    public TMP_InputField email;
    public RawImage photo;

    [Header("Pop-Up Object")]
    public GameObject successUI;
    public GameObject alertUI;
    public TMP_Text alertText;
    public GameObject failUI;
    public TMP_Text failText;

    public void DataBaseConnection()
    {
        string connString = $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";
        OracleConnection connection = new OracleConnection(connString);
        try
        {
            connection.Open();
            OracleCommand command = new OracleCommand("INSERT_MEMBER", connection);
            command.CommandType = CommandType.StoredProcedure;

            try
            {
                byte[] photo = null;
                if (this.photo.texture != null)
                {
                    Texture2D texture = this.photo.texture as Texture2D;
                    photo = texture.EncodeToPNG();
                }

                DateTime birth;
                if (!DateTime.TryParse(this.birth.text, out birth))
                {
                    failText.text = "생년월일 입력 형식이\n잘못되었습니다.";
                    failUI.SetActive(true);
                    connection.Close();
                    return;
                }

                char sex = char.ToUpper(this.sex.text[0]);
                if (sex == 'M' || sex == '남')
                {
                    sex = 'M';
                }
                else if (sex == 'F' || sex == 'W' || sex == '여')
                {
                    sex = 'F';
                }
                else
                {
                    failText.text = "성별 입력 형식이\n잘못되었습니다.";
                    failUI.SetActive(true);
                    connection.Close();
                    return;
                }

                string tel = this.tel.text.Replace("-", "").Replace(" ", "");
                tel = tel.StartsWith("0") ? tel.Substring(1) : tel;
                OracleCommand telselect = new OracleCommand($"SELECT * FROM MEMBER WHERE TEL = {tel}", connection);
                OracleDataReader reader = telselect.ExecuteReader();
                if (reader.HasRows)
                {
                    failText.text = "이미 등록된\n전화번호입니다.";
                    failUI.SetActive(true);
                    connection.Close();
                    return;
                }

                OracleCommand emailselect = new OracleCommand($"SELECT * FROM MEMBER WHERE EMAIL = '{email.text}'", connection);
                reader = emailselect.ExecuteReader();
                if (reader.HasRows)
                {
                    failText.text = "이미 등록된\n이메일입니다.";
                    failUI.SetActive(true);
                    connection.Close();
                    return;
                }

                command.Parameters.Add("M_NAME", OracleDbType.NVarchar2).Value = username.text;
                command.Parameters.Add("M_BIRTH", OracleDbType.Date).Value = birth;
                command.Parameters.Add("M_SEX", OracleDbType.Char).Value = sex;
                command.Parameters.Add("M_TEL", OracleDbType.Decimal).Value = tel;
                command.Parameters.Add("M_EMAIL", OracleDbType.Varchar2).Value = (string.IsNullOrEmpty(email.text)) ? DBNull.Value : email.text;
                command.Parameters.Add("M_PHOTO", OracleDbType.Blob).Value = (photo == null) ? DBNull.Value : photo;

                command.ExecuteNonQuery();

                successUI.SetActive(true);
            }
            catch (Exception ex)
            {
                alertText.text = "빈칸이 존재하거나\n입력 형식이 잘못되었습니다.";
                alertUI.SetActive(true);
            }
        }
        catch (Exception ex)
        {
            failText.text = "DB 연결에 실패했습니다.";
            failUI.SetActive(true);
        }
        finally
        {
            connection.Close();
        }
    }
}
