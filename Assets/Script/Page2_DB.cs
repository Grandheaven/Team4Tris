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

    static public bool run = false;
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

    [Header("Pop-Up Object")]
    public GameObject successUI;
    public GameObject alertUI;
    public TMP_Text alertText;
    public GameObject failUI;
    public TMP_Text failText;

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
    }

    public void MemberInformation()
    {
        string tel = datatel.Replace("-", "").Replace(" ", "");
        tel = tel.StartsWith("0") ? tel.Substring(1) : tel;

        string connString = $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";
        OracleConnection connection = new OracleConnection(connString);
        try
        {
            connection.Open();
            string sql = $"SELECT NAME, BIRTH, SEX, TEL, EMAIL, PHOTO FROM MEMBER WHERE TEL = {tel}"; // ���� ���̺� �̸����� ����
            OracleCommand command = new OracleCommand(sql, connection);
            OracleDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                username.text = ReadString(reader["NAME"]);
                birth.text = ReadString(reader["BIRTH"]);
                sex.text = ReadString(reader["SEX"]) == "M" ? "����" : "����";
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
                            Debug.LogError("�̹��� ������ �ε� ����: byte[]�� ��ȿ�� �̹��� ������ �ƴմϴ�.");
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
        catch (Exception ex) // DB ���� ���� �� ���� ó��
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
        string tel = datatel.Replace("-", "").Replace(" ", "");
        tel = tel.StartsWith("0") ? tel.Substring(1) : tel;

        string connString = $"Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={host})(PORT={port})))(CONNECT_DATA=(SID={sid})));User Id={userid};Password={password};";
        OracleConnection connection = new OracleConnection(connString);
        try
        {
            connection.Open();
            string sql = $"SELECT NAME, BIRTH, SEX, TEL, EMAIL, PHOTO FROM MEMBER WHERE TEL = {tel}"; // ���� ���̺� �̸����� ����
            OracleCommand command = new OracleCommand(sql, connection);
            OracleDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                username2.text = ReadString(reader["NAME"]);
                birth2.text = ReadString(reader["BIRTH"]);
                sex2.text = ReadString(reader["SEX"]) == "M" ? "����" : "����";
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
                            Debug.LogError("�̹��� ������ �ε� ����: byte[]�� ��ȿ�� �̹��� ������ �ƴմϴ�.");
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
        catch (Exception ex) // DB ���� ���� �� ���� ó��
        {
            Debug.LogError("Database connection failed: " + ex.Message);
        }
        finally
        {
            connection.Close();
            Debug.Log("Database connection closed.");
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
                    failText.text = "������� �Է� ������\n�߸��Ǿ����ϴ�.";
                    failUI.SetActive(true);
                    connection.Close();
                    return;
                }

                char sex = char.ToUpper(this.sex2.text[0]);
                if (sex == 'M' || sex == '��')
                {
                    sex = 'M';
                }
                else if (sex == 'F' || sex == 'W' || sex == '��')
                {
                    sex = 'F';
                }
                else
                {
                    failText.text = "���� �Է� ������\n�߸��Ǿ����ϴ�.";
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
                    failText.text = "�̹� ��ϵ�\n��ȭ��ȣ�Դϴ�.";
                    failUI.SetActive(true);
                    connection.Close();
                    return;
                }

                OracleCommand emailselect = new OracleCommand($"SELECT * FROM MEMBER WHERE EMAIL = '{email2.text}'", connection);
                reader = emailselect.ExecuteReader();
                if (reader.HasRows)
                {
                    failText.text = "�̹� ��ϵ�\n�̸����Դϴ�.";
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
                alertText.text = "��ĭ�� �����ϰų�\n�Է� ������ �߸��Ǿ����ϴ�.";
                alertUI.SetActive(true);
            }
        }
        catch (Exception ex)
        {
            failText.text = "DB ���ῡ �����߽��ϴ�.";
            failUI.SetActive(true);
        }
        finally
        {
            connection.Close();
        }
    }

    private string ReadString(object dbValue)
    {
        return (dbValue == DBNull.Value || dbValue == null) ? string.Empty : dbValue.ToString();
    }

}
