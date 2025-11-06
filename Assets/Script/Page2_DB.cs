using UnityEngine;
using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using TMPro;
using UnityEngine.UI;

public class Page2_DB : MonoBehaviour
{
    private string host = "deu.duraka.shop";
    private string port = "4264";
    private string sid = "xe";
    private string userid = "TEAM4";
    private string password = "Team4Tris";

    private string savetel;
    private string saveemail;

    static public bool run = false;
    static public bool delete = false;
    static public string dataname;
    static public string databirth;
    static public string datasex;
    static public string datatel;

    [Header("MemberInformation Object")]
    public GameObject memberinfo;
    public TMP_InputField username;
    public TMP_InputField birth;
    public TMP_InputField sex;
    public TMP_InputField tel;
    public TMP_InputField email;
    public RawImage photo;

    [Header("MemberModify Object")]
    public GameObject memberinfo2;
    public TMP_InputField username2;
    public TMP_InputField birth2;
    public TMP_InputField sex2;
    public TMP_InputField tel2;
    public TMP_InputField email2;
    public RawImage photo2;
    public GameObject background1;
    public GameObject background2;
    public GameObject background3;

    [Header("Pop-Up Object")]
    public GameObject successUI;
    public GameObject alertUI;
    public TMP_Text alertText;
    public GameObject failUI;
    public TMP_Text failText;
    public GameObject deleteUI;

    void Update()
    {
        if (memberinfo.activeInHierarchy && run)
        {
            MemberInformation();
            run = false;
        }
        if (memberinfo2.activeInHierarchy && run)
        {
            MemberModify();
            run = false;
        }
        if (delete == true && run == true)
        {
            MemberDelete();
            run = false;
        }
    }

    public void MemberInformation()
    {
        background1.SetActive(false);
        background2.SetActive(false);
        background3.SetActive(false);

        string tel = datatel.Replace("-", "").Replace(" ", "");
        tel = tel.StartsWith("0") ? tel.Substring(1) : tel;

        string connString = $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";
        OracleConnection connection = new OracleConnection(connString);
        try
        {
            connection.Open();
            string sql = $"SELECT NAME, BIRTH, SEX, TEL, EMAIL, PHOTO FROM MEMBER WHERE TEL = {tel}"; // 실제 테이블 이름으로 변경
            OracleCommand command = new OracleCommand(sql, connection);
            OracleDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                username.text = ReadString(reader["NAME"]);
                birth.text = ReadString(reader["BIRTH"]);
                sex.text = ReadString(reader["SEX"]) == "M" ? "남성" : "여성";
                this.tel.text = datatel;
                email.text = ReadString(reader["EMAIL"]);

                if (reader["PHOTO"] != DBNull.Value)
                {
                    byte[] imageData = (byte[])reader["PHOTO"];

                    if (imageData != null && imageData.Length > 0)
                    {
                        Texture2D texture = new Texture2D(2, 2);
                        if (texture.LoadImage(imageData))
                        {
                            photo.texture = texture;
                            photo.color = Color.white;
                        }
                        else
                        {
                            Debug.LogError("이미지 데이터 로드 실패: byte[]가 유효한 이미지 형식이 아닙니다.");
                            photo.texture = null;
                        }
                    }
                    else
                    {
                        photo.texture = null;
                    }
                }
                else
                {
                    photo.texture = null;
                }
            }
        }
        catch (Exception ex) // DB 연결 실패 시 예외 처리
        {
            Debug.LogError("Database connection failed: " + ex.Message);
        }
        finally
        {
            connection.Close();
            Debug.Log("Database connection closed.");
        }
    }

    public void MemberModify()
    {
        background1.SetActive(false);
        background2.SetActive(false);
        background3.SetActive(false);

        string tel = datatel.Replace("-", "").Replace(" ", "");
        tel = tel.StartsWith("0") ? tel.Substring(1) : tel;

        string connString = $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";
        OracleConnection connection = new OracleConnection(connString);
        try
        {
            connection.Open();
            string sql = $"SELECT NAME, BIRTH, SEX, TEL, EMAIL, PHOTO FROM MEMBER WHERE TEL = {tel}"; // 실제 테이블 이름으로 변경
            OracleCommand command = new OracleCommand(sql, connection);
            OracleDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                username2.text = ReadString(reader["NAME"]);
                birth2.text = ReadString(reader["BIRTH"]);
                sex2.text = ReadString(reader["SEX"]) == "M" ? "남성" : "여성";
                this.tel2.text = datatel;
                email2.text = ReadString(reader["EMAIL"]);

                if (reader["PHOTO"] != DBNull.Value)
                {
                    byte[] imageData = (byte[])reader["PHOTO"];

                    if (imageData != null && imageData.Length > 0)
                    {
                        Texture2D texture = new Texture2D(2, 2);
                        if (texture.LoadImage(imageData))
                        {
                            photo2.texture = texture;
                            photo2.color = Color.white;
                        }
                        else
                        {
                            Debug.LogError("이미지 데이터 로드 실패: byte[]가 유효한 이미지 형식이 아닙니다.");
                            photo2.texture = null;
                        }
                    }
                    else
                    {
                        photo2.texture = null;
                    }
                }
                else
                {
                    photo2.texture = null;
                }
            }
        }
        catch (Exception ex) // DB 연결 실패 시 예외 처리
        {
            Debug.LogError("Database connection failed: " + ex.Message);
        }
        finally
        {
            connection.Close();
            Debug.Log("Database connection closed.");

            savetel = tel2.text;
            saveemail = email2.text;
        }
    }

    public void DataBaseConnection()
    {
        string connString = $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";
        OracleConnection connection = new OracleConnection(connString);
        try
        {
            connection.Open();
            OracleCommand command = new OracleCommand("UPDATE_MEMBER", connection);
            command.CommandType = CommandType.StoredProcedure;

            try
            {
                byte[] photo = null;
                if (this.photo2.texture != null)
                {
                    Texture2D texture = this.photo2.texture as Texture2D;
                    photo = texture.EncodeToPNG();
                }

                DateTime birth;
                if (!DateTime.TryParse(this.birth2.text, out birth))
                {
                    failText.text = "생년월일 입력 형식이\n잘못되었습니다.";
                    failUI.SetActive(true);
                    connection.Close();
                    return;
                }

                char sex = char.ToUpper(this.sex2.text[0]);
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

                string tel = this.tel2.text.Replace("-", "").Replace(" ", "");
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

                OracleCommand emailselect = new OracleCommand($"SELECT * FROM MEMBER WHERE EMAIL = '{email2.text}'", connection);
                reader = emailselect.ExecuteReader();
                if (reader.HasRows)
                {
                    failText.text = "이미 등록된\n이메일입니다.";
                    failUI.SetActive(true);
                    connection.Close();
                    return;
                }

                command.Parameters.Add("M_NAME", OracleDbType.NVarchar2).Value = username2.text;
                command.Parameters.Add("M_BIRTH", OracleDbType.Date).Value = birth;
                command.Parameters.Add("M_SEX", OracleDbType.Char).Value = sex;
                command.Parameters.Add("M_TEL", OracleDbType.Decimal).Value = tel;
                command.Parameters.Add("M_EMAIL", OracleDbType.Varchar2).Value = (string.IsNullOrEmpty(email2.text)) ? DBNull.Value : email2.text;
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

    public void MemberUpdate()
    {
        string connString = $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";
        OracleConnection connection = new OracleConnection(connString);
        try
        {
            connection.Open();
            OracleCommand command = new OracleCommand("UPDATE_MEMBER", connection);
            command.CommandType = CommandType.StoredProcedure;

            string sql = $"SELECT MNO FROM MEMBER WHERE TEL = {savetel}";
            OracleCommand mno = new OracleCommand(sql, connection);
            OracleDataReader reader = mno.ExecuteReader();

            try
            {
                byte[] photo = null;
                if (this.photo2.texture != null)
                {
                    Texture2D texture = this.photo2.texture as Texture2D;
                    photo = texture.EncodeToPNG();
                }

                DateTime birth;
                if (!DateTime.TryParse(this.birth2.text, out birth))
                {
                    failText.text = "생년월일 입력 형식이\n잘못되었습니다.";
                    failUI.SetActive(true);
                    connection.Close();
                    return;
                }

                char sex = char.ToUpper(this.sex2.text[0]);
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

                string tel = this.tel2.text.Replace("-", "").Replace(" ", "");
                tel = tel.StartsWith("0") ? tel.Substring(1) : tel;
                OracleCommand telselect = new OracleCommand($"SELECT * FROM MEMBER WHERE TEL = {tel}", connection);
                OracleDataReader telreader = telselect.ExecuteReader();
                if (telreader.HasRows && savetel != tel2.text)
                {
                    failText.text = "이미 등록된\n전화번호입니다.";
                    failUI.SetActive(true);
                    connection.Close();
                    return;
                }

                OracleCommand emailselect = new OracleCommand($"SELECT * FROM MEMBER WHERE EMAIL = '{email2.text}'", connection);
                reader = emailselect.ExecuteReader();
                if (reader.HasRows && saveemail != email2.text)
                {
                    failText.text = "이미 등록된\n이메일입니다.";
                    failUI.SetActive(true);
                    connection.Close();
                    return;
                }

                if (reader.Read()) command.Parameters.Add("M_MNO", OracleDbType.Decimal).Value = ReadString(reader["MNO"]);
                command.Parameters.Add("M_NAME", OracleDbType.NVarchar2).Value = username2.text;
                command.Parameters.Add("M_BIRTH", OracleDbType.Date).Value = birth;
                command.Parameters.Add("M_SEX", OracleDbType.Char).Value = sex;
                command.Parameters.Add("M_TEL", OracleDbType.Decimal).Value = tel;
                command.Parameters.Add("M_EMAIL", OracleDbType.Varchar2).Value = (string.IsNullOrEmpty(email2.text)) ? DBNull.Value : email.text;
                command.Parameters.Add("M_PHOTO", OracleDbType.Blob).Value = (photo == null) ? DBNull.Value : photo;

                command.ExecuteNonQuery();

                successUI.SetActive(true);
            }
            catch (Exception ex)
            {
                alertText.text = "빈칸이 존재하거나\n입력 형식이 잘못되었습니다.";
                alertUI.SetActive(true);
                successUI.SetActive(false);
            }
        }
        catch (Exception ex) // DB 연결 실패 시 예외 처리
        {
            Debug.LogError("Database connection failed: " + ex.Message);
        }
        finally
        {
            connection.Close();
            Debug.Log("Database connection closed.");
        }
    }

    public void MemberDelete()
    {
        Debug.Log(dataname + "!!");
        Debug.Log(databirth + "!!");
        Debug.Log(datasex + "!!");
        Debug.Log(datatel + "!!");

        background1.SetActive(false);
        background2.SetActive(false);
        background3.SetActive(false);

        string connString = $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";
        OracleConnection connection = new OracleConnection(connString);

        string tel = datatel.Replace("-", "").Replace(" ", "");
        tel = tel.StartsWith("0") ? tel.Substring(1) : tel;

        try
        {
            connection.Open();
            OracleCommand command = new OracleCommand("DELETE_MEMBER", connection);
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("M_NAME", OracleDbType.NVarchar2).Value = dataname;
            command.Parameters.Add("M_BIRTH", OracleDbType.Date).Value = databirth;
            command.Parameters.Add("M_SEX", OracleDbType.Char).Value = datasex == "남성" ? "M" : "F";
            command.Parameters.Add("M_TEL", OracleDbType.Decimal).Value = (string.IsNullOrEmpty(tel)) ? DBNull.Value : tel;

            command.ExecuteNonQuery();

            deleteUI.SetActive(true);
        }
        catch (Exception ex) // DB 연결 실패 시 예외 처리
        {
            alertUI.SetActive(true);
            deleteUI.SetActive(false);
        }
        finally
        {
            connection.Close();
            Debug.Log("Database connection closed.");
        }
    }

    private string ReadString(object dbValue)
    {
        return (dbValue == DBNull.Value || dbValue == null) ? string.Empty : dbValue.ToString();
    }

    public void SceneInit()
    {
        Page2 active = new Page2();
        active.Click();
    }
}
